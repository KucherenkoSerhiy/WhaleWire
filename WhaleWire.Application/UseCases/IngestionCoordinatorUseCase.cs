using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Persistence;

namespace WhaleWire.Application.UseCases;

public sealed class IngestionCoordinatorUseCase(
    IIngestorUseCase ingestorUseCase,
    IMonitoredAddressRepository monitoredAddressRepository,
    IBlockchainClient blockchainClient) : IIngestionCoordinatorUseCase
{
    public async Task<IngestionResult> ExecuteAsync(CancellationToken ct = default)
    {
        var addresses = await monitoredAddressRepository.GetActiveAddressesAsync(
            blockchainClient.Chain,
            blockchainClient.Provider,
            ct);

        if (addresses.Count == 0)
            return new IngestionResult(0, 0, []);

        var results = new List<AddressResult>();
        var totalEventsPublished = 0;

        foreach (var address in addresses)
        {
            try
            {
                var eventsPublished = await ingestorUseCase.ExecuteAsync(address, ct);
                totalEventsPublished += eventsPublished;
                results.Add(new AddressResult(address, eventsPublished, null));
            }
            catch (Exception ex)
            {
                results.Add(new AddressResult(address, 0, ex.Message));
            }
        }

        return new IngestionResult(addresses.Count, totalEventsPublished, results);
    }
}

public sealed record IngestionResult(
    int AddressesProcessed,
    int TotalEventsPublished,
    IReadOnlyList<AddressResult> Results);

public sealed record AddressResult(
    string Address,
    int EventsPublished,
    string? Error);
