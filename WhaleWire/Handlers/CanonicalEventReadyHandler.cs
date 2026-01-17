using Microsoft.Extensions.Options;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Messages;
using Polly;
using Polly.CircuitBreaker;
using WhaleWire.Configuration;

namespace WhaleWire.Handlers;

public sealed class CanonicalEventReadyHandler(
    IEventRepository eventRepository,
    ICheckpointRepository  checkpointRepository,
    ILogger<CanonicalEventReadyHandler> logger,
    IOptions<CircuitBreakerOptions> options)
    : IMessageConsumer<CanonicalEventReady>
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
        .Handle<Exception>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: options.Value.ExceptionsAllowedBeforeBreaking,
            durationOfBreak: TimeSpan.FromMinutes(options.Value.DurationOfBreakMinutes));

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

            if (wasInserted)
            {
                await checkpointRepository.UpdateCheckpointMonotonicAsync(
                    message.Chain,
                    message.Address,
                    message.Provider,
                    message.Lt,
                    message.TxHash,
                    token);
            }

            logger.LogInformation(
                wasInserted
                    ? "Event {EventId} inserted successfully"
                    : "Event {EventId} already exists, skipped (idempotent)", message.EventId);
        });
    }
}