namespace WhaleWire.Infrastructure.Persistence.Entities;

public sealed class MonitoredAddress
{
    public required string Chain { get; init; }
    public required string Address { get; init; }
    public required string Provider { get; init; }
    public string Balance { get; set; } = "0";
    public bool IsActive { get; set; } = true;
    public DateTime DiscoveredAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
