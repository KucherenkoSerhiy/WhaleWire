using Microsoft.Extensions.Options;
using WhaleWire.Configuration;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection(SchedulerOptions.SectionName));

// Infrastructure
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres") 
    ?? throw new InvalidOperationException("Postgres connection string is required");
builder.Services.AddPersistence(postgresConnectionString);

// Services
builder.Services.AddHostedService<SchedulerService>();

var host = builder.Build();

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
