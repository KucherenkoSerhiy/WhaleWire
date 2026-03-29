using Microsoft.EntityFrameworkCore;
using Prometheus;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;
using WhaleWire.Configuration;
using WhaleWire.Handlers;
using WhaleWire.Infrastructure.Ingestion;
using WhaleWire.Infrastructure.Ingestion.Configuration;
using WhaleWire.Infrastructure.Messaging;
using WhaleWire.Infrastructure.Notifications;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Messages;
using WhaleWire.Application.Metrics;
using WhaleWire.Metrics;
using WhaleWire.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection(SchedulerOptions.SectionName));
builder.Services.Configure<CircuitBreakerOptions>(
    builder.Configuration.GetSection(CircuitBreakerOptions.SectionName));
builder.Services.Configure<HealthOptions>(
    builder.Configuration.GetSection(HealthOptions.SectionName));
builder.Services.Configure<MetricsCollectorOptions>(
    builder.Configuration.GetSection(MetricsCollectorOptions.SectionName));

// Infrastructure - Ingestion
builder.Services.AddIngestion(builder.Configuration);

// Application - Use Cases
builder.Services.AddScoped<IIngestorUseCase>(sp =>
{
    var tonApiOptions = sp.GetRequiredService<IOptions<TonApiOptions>>().Value;
    return new IngestorUseCase(
        sp.GetRequiredService<IBlockchainClient>(),
        sp.GetRequiredService<ILeaseRepository>(),
        sp.GetRequiredService<ICheckpointRepository>(),
        sp.GetRequiredService<IMessagePublisher>(),
        tonApiOptions.DelayBetweenRequestsMs);
});
builder.Services.AddScoped<IDiscoveryUseCase, DiscoveryUseCase>();
builder.Services.AddScoped<IIngestionCoordinatorUseCase, IngestionCoordinatorUseCase>();

// Infrastructure - Persistence
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is required");
builder.Services.AddPersistence(postgresConnectionString);

// Infrastructure - Messaging
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? throw new InvalidOperationException("RabbitMQ connection string is required");
builder.Services.AddMessaging(builder.Configuration, rabbitMqConnectionString);

// Infrastructure - Notifications
builder.Services.AddNotifications();

// Metrics
builder.Services.AddSingleton<IWhaleWireMetrics, WhaleWireMetrics>();

// Health checks - RabbitMQ requires IConnection (v9+); register singleton for health check.
// Retry: after empty volumes / compose up, the broker can briefly refuse TCP even when depends_on health passes.
builder.Services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
    CreateRabbitMqConnectionWithRetry(sp, rabbitMqConnectionString));
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnectionString, name: "postgres")
    .AddRabbitMQ(name: "rabbitmq");

// Message consumers
builder.Services.AddMessageConsumer<BlockchainEvent, BlockchainEventHandler>();

// Hosted services
builder.Services.AddHostedService<SchedulerService>();
builder.Services.AddHostedService<EventLagMetricsCollector>();
builder.Services.AddHostedService<DlqMetricsCollector>();

var app = builder.Build();

// Migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WhaleWireDbContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        migrationLogger.LogInformation("Migrating database...");
        await dbContext.Database.MigrateAsync();
        migrationLogger.LogInformation("Migrated database successfully.");
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "Failed to migrate database.");
        throw;
    }

    // Publish monitored-wallet gauge immediately so Prometheus/Grafana always see whalewire_monitored_addresses
    // (discovery may fail or run later; unset gauges can be omitted from scrape output).
    try
    {
        var metrics = scope.ServiceProvider.GetRequiredService<IWhaleWireMetrics>();
        var monitoredRepo = scope.ServiceProvider.GetRequiredService<IMonitoredAddressRepository>();
        var blockchainClient = scope.ServiceProvider.GetRequiredService<IBlockchainClient>();
        var initialCount = await monitoredRepo.CountActiveDistinctAddressesAsync(
            blockchainClient.Chain,
            blockchainClient.Provider);
        metrics.RecordActiveMonitoredAddressCount(initialCount);
        migrationLogger.LogInformation(
            "Published whalewire_monitored_addresses initial value: {Count}",
            initialCount);
    }
    catch (Exception ex)
    {
        migrationLogger.LogWarning(ex, "Could not publish initial whalewire_monitored_addresses (metric may be missing until discovery runs).");
    }
}

// Health endpoint
var healthOptions = app.Services.GetRequiredService<IOptions<HealthOptions>>().Value;
app.MapHealthChecks(healthOptions.Path);

// Prometheus metrics
app.UseHttpMetrics();
app.MapMetrics();

// Log startup and config summary (no secrets)
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var schedulerOptions = app.Services.GetRequiredService<IOptions<SchedulerOptions>>().Value;

logger.LogInformation("WhaleWire starting...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("Configuration summary:");
logger.LogInformation("  Scheduler.Enabled: {Enabled}", schedulerOptions.Enabled);
logger.LogInformation("  Scheduler.PollingIntervalSeconds: {PollingInterval}", schedulerOptions.PollingIntervalSeconds);
logger.LogInformation("  Health: {Path}", healthOptions.Path);
logger.LogInformation("  Postgres: {Status}", string.IsNullOrEmpty(postgresConnectionString) ? "Not configured" : "Configured");

await app.RunAsync();

public partial class Program
{
    private static RabbitMQ.Client.IConnection CreateRabbitMqConnectionWithRetry(
        IServiceProvider services,
        string rabbitMqConnectionString)
    {
        var factory = new RabbitMQ.Client.ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("WhaleWire.RabbitMqConnection");
        const int maxAttempts = 15;
        var delay = TimeSpan.FromSeconds(2);
        Exception? last = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                last = ex;
                if (attempt >= maxAttempts)
                    break;
                logger.LogWarning(
                    ex,
                    "RabbitMQ not reachable (attempt {Attempt}/{Max}); retrying in {Delay}s",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException(
            $"Could not connect to RabbitMQ after {maxAttempts} attempts (~{maxAttempts * delay.TotalSeconds:F0}s).",
            last);
    }
}
