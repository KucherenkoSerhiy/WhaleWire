using Microsoft.Extensions.Options;
using WhaleWire.Configuration;
using WhaleWire.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<SchedulerOptions>(
    builder.Configuration.GetSection(SchedulerOptions.SectionName));
builder.Services.AddHostedService<SchedulerService>();

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var schedulerOptions = host.Services.GetRequiredService<IOptions<SchedulerOptions>>().Value;

logger.LogInformation("WhaleWire starting...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("Configuration summary:");
logger.LogInformation("  Scheduler.Enabled: {Enabled}", schedulerOptions.Enabled);
logger.LogInformation("  Scheduler.PollingIntervalSeconds: {PollingInterval}", schedulerOptions.PollingIntervalSeconds);

await host.RunAsync();
