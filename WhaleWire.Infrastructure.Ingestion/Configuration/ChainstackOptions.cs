namespace WhaleWire.Infrastructure.Ingestion.Configuration;

public sealed class ChainstackOptions
{
    public const string SectionName = "Chainstack";

    public required string ApiUrl { get; init; }
}
