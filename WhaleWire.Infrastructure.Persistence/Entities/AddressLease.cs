namespace WhaleWire.Infrastructure.Persistence.Entities;

public sealed class AddressLease
{
    public required string LeaseKey { get; init; }
    public required string OwnerId { get; set; }
    public required DateTime ExpiresAt { get; set; }

    public bool BelongsTo(string ownerId)
    {
        return OwnerId == ownerId;
    }

    public bool IsHeldByAnotherOwner(DateTime now, string ownerId)
    {
        return ExpiresAt > now && !BelongsTo(ownerId);
    }

    public void Renew(string ownerId, DateTime expiresAt)
    {
        OwnerId = ownerId;
        ExpiresAt = expiresAt;
    }
}