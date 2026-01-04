using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Messages;
using Polly;
using Polly.CircuitBreaker;

namespace WhaleWire.Handlers;

public sealed class CanonicalEventReadyHandler(
    IEventRepository eventRepository,
    ILogger<CanonicalEventReadyHandler> logger)
    : IMessageConsumer<CanonicalEventReady>
{
    private static readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1));

    public async Task HandleAsync(CanonicalEventReady message, CancellationToken token = default)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            logger.LogInformation("Received CanonicalEventReady: {EventId} for {Chain}/{Address}",
                message.EventId, message.Chain, message.Address);

            var wasInserted = await eventRepository.UpsertEventIdempotentAsync(
                eventId: message.EventId,
                chain: message.Chain,
                address: message.Address,
                lt: message.Lt,
                txHash: message.TxHash,
                blockTime: message.OccurredAt,
                rawJson: message.RawJson,
                ct: token);

            logger.LogInformation(
                wasInserted
                    ? "Event {EventId} inserted successfully"
                    : "Event {EventId} already exists, skipped (idempotent)", message.EventId);
        });
    }
}