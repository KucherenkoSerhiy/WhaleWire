using Microsoft.EntityFrameworkCore;
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
using WhaleWire.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection(SchedulerOptions.SectionName));
builder.Services.Configure<CircuitBreakerOptions>(
    builder.Configuration.GetSection(CircuitBreakerOptions.SectionName));
builder.Services.Configure<HealthOptions>(
    builder.Configuration.GetSection(HealthOptions.SectionName));

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

// Health checks - RabbitMQ requires IConnection (v9+); register singleton for health check
builder.Services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
{
    var factory = new RabbitMQ.Client.ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnectionString, name: "postgres")
    .AddRabbitMQ(name: "rabbitmq");

// Message consumers
builder.Services.AddMessageConsumer<BlockchainEvent, BlockchainEventHandler>();

// Hosted services
builder.Services.AddHostedService<SchedulerService>();

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
}

// Health endpoint
var healthOptions = app.Services.GetRequiredService<IOptions<HealthOptions>>().Value;
app.MapHealthChecks(healthOptions.Path);

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
