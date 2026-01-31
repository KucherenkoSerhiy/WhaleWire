using Microsoft.Extensions.Options;
using WhaleWire.Application.Messaging;
using WhaleWire.Configuration;
using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Services;

public sealed class SchedulerService(
    ILogger<SchedulerService> logger,
    IOptions<SchedulerOptions> options,
    IMessagePublisher messagePublisher)
    : BackgroundService
{
    private readonly SchedulerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SchedulerService is disabled by configuration");
            return;
        }

        logger.LogInformation(
            "SchedulerService started with polling interval of {PollingInterval} seconds",
            _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteScheduledWorkAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during scheduled work execution");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("SchedulerService stopped");
    }

    // TODO: implement actual tests
    // Fixed event for idempotency testing
    private static readonly BlockchainEvent TestEvent = new()
    {
        EventId = "test-idempotency-event-001",
        Chain = "ton-testnet",
        Provider = "test-provider",
        Address = "EQTest1234567890abcdef",
        Cursor = new Cursor(12345678L, "abc123def456"),
        RawJson = """{"test": true, "message": "idempotency test"}""",
        OccurredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
    
    private async Task ExecuteScheduledWorkAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Executing scheduled work cycle");
        await messagePublisher.PublishAsync(TestEvent, stoppingToken);
        
        logger.LogInformation(
            "Published CanonicalEventReady with EventId: {EventId}", 
            TestEvent.EventId);
    }
}

