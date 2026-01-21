namespace WhaleWire.Infrastructure.Ingestion.Configuration;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public bool Enabled { get; init; } = true;
    public int PollingIntervalSeconds { get; init; } = 30;
}
