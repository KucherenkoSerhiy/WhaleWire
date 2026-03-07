namespace WhaleWire.Configuration;

public sealed class HealthOptions
{
    public const string SectionName = "Health";

    public string Path { get; init; } = "/health";
}
