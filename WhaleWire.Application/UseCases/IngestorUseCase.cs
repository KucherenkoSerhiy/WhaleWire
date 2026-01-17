using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Domain.Services;
using WhaleWire.Messages;

namespace WhaleWire.Application.UseCases;

public sealed class IngestorUseCase(
    IBlockchainClient blockchainClient,
    ILeaseRepository leaseRepository,
    ICheckpointRepository checkpointRepository,
    IMessagePublisher messagePublisher) : IIngestorUseCase
{
    private const string OwnerId = "ingestor";

    public async Task<int> ExecuteAsync(string address, CancellationToken token = default)
    {
        var leaseKey = $"{blockchainClient.Chain}:{blockchainClient.Provider}:{address}";

        var leaseAcquired = await leaseRepository.TryAcquireLeaseAsync(
            leaseKey, OwnerId, TimeSpan.FromMinutes(5), token);

        if (!leaseAcquired)
            return 0;

        try
        {
            var events = await GetRawChainEvents(address, token);
            await PublishEvents(token, events);
            
            return events.Count;
        }
        finally
        {
            await leaseRepository.ReleaseLeaseAsync(leaseKey, OwnerId, token);
        }
    }

    private async Task<IReadOnlyList<RawChainEvent>> GetRawChainEvents(string address, CancellationToken token)
    {
        var checkpointData = await checkpointRepository.GetCheckpointAsync(
            blockchainClient.Chain, address, blockchainClient.Provider, token);
            
        var cursor = checkpointData is not null
            ? new Cursor(checkpointData.LastLt, checkpointData.LastHash)
            : null;

        var events = await blockchainClient.GetEventsAsync(address, cursor, limit: 100, token);
        return events;
    }

    private async Task PublishEvents(CancellationToken token, IReadOnlyList<RawChainEvent> events)
    {
        foreach (var @event in events)
        {
            var message = new CanonicalEventReady
            {
                EventId = EventIdGenerator.Generate(@event.Chain, @event.Address, @event.Cursor.Primary, @event.Hash),
                Chain = @event.Chain,
                Provider = @event.Provider,
                Address = @event.Address,
                Lt = @event.Cursor.Primary,
                TxHash = @event.Hash,
                RawJson = @event.RawJson,
                OccurredAt = @event.OccurredAt
            };
                
            await messagePublisher.PublishAsync(message, token);
        }
    }
}