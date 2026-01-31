using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Application.UseCases;

public sealed class IngestorUseCase(
    IBlockchainClient blockchainClient,
    ILeaseRepository leaseRepository,
    ICheckpointRepository checkpointRepository,
    IMessagePublisher messagePublisher,
    int delayBetweenRequestsMs) : IIngestorUseCase
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
            var checkpointData = await checkpointRepository.GetCheckpointAsync(
                blockchainClient.Chain, address, blockchainClient.Provider, token);
            
            var cursor = checkpointData is not null
                ? new Cursor(checkpointData.LastLt, checkpointData.LastHash)
                : null;

            var events = await blockchainClient.GetEventsAsync(address, cursor, limit: 100, token);
            
            await LimitRate(token);
            
            foreach (var evt in events)
            {
                await messagePublisher.PublishAsync(evt, token);
            }

            return events.Count;
        }
        finally
        {
            await leaseRepository.ReleaseLeaseAsync(leaseKey, OwnerId, token);
        }
    }

    private async Task LimitRate(CancellationToken token)
    {
        await Task.Delay(delayBetweenRequestsMs, token);
    }
}