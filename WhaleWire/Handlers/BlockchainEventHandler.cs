using Microsoft.Extensions.Options;
using WhaleWire.Application.Alerts;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Messages;
using Polly;
using Polly.CircuitBreaker;
using WhaleWire.Configuration;

namespace WhaleWire.Handlers;

public sealed class BlockchainEventHandler(
    IEventRepository eventRepository,
    ICheckpointRepository  checkpointRepository,
    IAlertEvaluator alertEvaluator,
    IAlertNotifier alertNotifier,
    ILogger<BlockchainEventHandler> logger,
    IOptions<CircuitBreakerOptions> options)
    : IMessageConsumer<BlockchainEvent>
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: options.Value.ExceptionsAllowedBeforeBreaking,
            durationOfBreak: TimeSpan.FromMinutes(options.Value.DurationOfBreakMinutes));

    public async Task HandleAsync(BlockchainEvent message, CancellationToken token = default)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            logger.LogInformation("Received CanonicalEventReady: {EventId} for {Chain}/{Address}",
                message.EventId, message.Chain, message.Address);

            var wasInserted = await eventRepository.UpsertEventIdempotentAsync(
                eventId: message.EventId,
                chain: message.Chain,
                address: message.Address,
                lt: message.Cursor.Primary,
                txHash: message.Cursor.Secondary,
                blockTime: message.OccurredAt,
                rawJson: message.RawJson,
                ct: token);

            if (wasInserted)
            {
                await checkpointRepository.UpdateCheckpointMonotonicAsync(
                    message.Chain,
                    message.Address,
                    message.Provider,
                    message.Cursor.Primary,
                    message.Cursor.Secondary,
                    token);

                // Evaluate and send alerts for new events
                var alerts = await alertEvaluator.EvaluateAsync(message, token);
                foreach (var alert in alerts)
                {
                    await alertNotifier.NotifyAsync(alert, token);
                }
            }

            logger.LogInformation(
                wasInserted
                    ? "Event {EventId} inserted successfully"
                    : "Event {EventId} already exists, skipped (idempotent)", message.EventId);
        });
    }
}