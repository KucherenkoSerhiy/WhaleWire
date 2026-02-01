using System.Numerics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.UseCases;
using WhaleWire.Infrastructure.Ingestion.Configuration;

namespace WhaleWire.Infrastructure.Ingestion.Clients;

public sealed class CompositeTopAccountsClient(
    IEnumerable<IAssetTopHoldersProvider> assetProviders,
    IOptions<TonCenterOptions> tonCenterOptions,
    ILogger<CompositeTopAccountsClient> logger) : ITopAccountsClient
{
    private readonly int _delayMs = tonCenterOptions.Value.DelayBetweenRequestsMs;
    public async Task<IReadOnlyList<AssetTopHolders>> GetTopAccountsByAssetAsync(
        int limit,
        CancellationToken ct = default)
    {
        var results = new List<AssetTopHolders>();
        var successCount = 0;
        var failCount = 0;

        foreach (var provider in assetProviders)
        {
            AssetTopHolders? assetHolders = null;
            try
            {
                assetHolders = await provider.GetTopHoldersAsync(limit, ct);
                successCount++;
                
                // Delay between providers to respect rate limit
                if (successCount + failCount < assetProviders.Count())
                {
                    await Task.Delay(_delayMs, ct);
                }

                logger.LogInformation(
                    "Fetched {Count} holders for asset {Asset} ({Type})",
                    assetHolders.Holders.Count, assetHolders.AssetIdentifier, assetHolders.AssetType);

                results.Add(assetHolders);
            }
            catch (Exception ex)
            {
                failCount++;
                var assetName = assetHolders?.AssetIdentifier ?? provider.GetType().Name;
                logger.LogWarning(ex, 
                    "Failed to fetch holders from {Provider}: {Message}", 
                    assetName, ex.Message);
            }
        }

        var totalHolders = results.Sum(r => r.Holders.Count);
        logger.LogInformation(
            "Discovery complete: {Success} providers succeeded, {Fail} failed, {Total} total holders",
            successCount, failCount, totalHolders);

        if (results.Count == 0 && failCount > 0)
        {
            throw new InvalidOperationException("All asset providers failed");
        }

        return results;
    }
}
