using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Messages;

namespace WhaleWire.Handlers;

public sealed class CanonicalEventReadyHandler(
    IEventRepository eventRepository,
    ILogger<CanonicalEventReadyHandler> logger)
    : IMessageConsumer<CanonicalEventReady>
{
    public async Task HandleAsync(CanonicalEventReady message, CancellationToken token = default)
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
    }
}