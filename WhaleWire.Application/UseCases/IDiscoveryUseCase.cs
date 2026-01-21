namespace WhaleWire.Application.UseCases;

public interface IDiscoveryUseCase
{
    /// <summary>
    /// Discovers and updates top N monitored addresses by balance.
    /// Returns the count of addresses discovered.
    /// </summary>
    Task<int> ExecuteAsync(int limit, CancellationToken ct = default);
}
