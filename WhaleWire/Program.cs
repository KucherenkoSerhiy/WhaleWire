using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WhaleWire.Configuration;
using WhaleWire.Handlers;
using WhaleWire.Infrastructure.Messaging;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Messages;
using WhaleWire.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection(SchedulerOptions.SectionName));

// Infrastructure - Persistence
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres") 
    ?? throw new InvalidOperationException("Postgres connection string is required");
builder.Services.AddPersistence(postgresConnectionString);

// Infrastructure - Messaging
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? throw new InvalidOperationException("RabbitMQ connection string is required");
builder.Services.AddMessaging(builder.Configuration, rabbitMqConnectionString);

// Message consumers
builder.Services.AddMessageConsumer<CanonicalEventReady, CanonicalEventReadyHandler>();

// Hosted services
builder.Services.AddHostedService<SchedulerService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
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

// Log startup and config summary (no secrets)
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var schedulerOptions = host.Services.GetRequiredService<IOptions<SchedulerOptions>>().Value;

logger.LogInformation("WhaleWire starting...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("Configuration summary:");
logger.LogInformation("  Scheduler.Enabled: {Enabled}", schedulerOptions.Enabled);
logger.LogInformation("  Scheduler.PollingIntervalSeconds: {PollingInterval}", schedulerOptions.PollingIntervalSeconds);
logger.LogInformation("  Postgres: {Status}", string.IsNullOrEmpty(postgresConnectionString) ? "Not configured" : "Configured");

await host.RunAsync();
