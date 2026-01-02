namespace WhaleWire.Infrastructure.Persistence.Entities;

public sealed class AddressLease
{
    public required string LeaseKey { get; init; }
    public required string OwnerId { get; set; }
    public required DateTime ExpiresAt { get; set; }
}

