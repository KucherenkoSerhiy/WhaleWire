namespace WhaleWire.Application.UseCases;

public interface IIngestorUseCase
{
    /// <summary>
    /// Ingests new events for the given address.
    /// Returns the count of events published.
    /// </summary>
    Task<int> ExecuteAsync(string address, CancellationToken token = default);
}