namespace WhaleWire.Application.UseCases;

public interface IIngestionCoordinatorUseCase
{
    /// <summary>
    /// Coordinates ingestion for all monitored addresses.
    /// Returns ingestion statistics.
    /// </summary>
    Task<IngestionResult> ExecuteAsync(CancellationToken ct = default);
}
