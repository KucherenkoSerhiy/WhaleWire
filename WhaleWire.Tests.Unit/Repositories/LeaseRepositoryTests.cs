using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using WhaleWire.Infrastructure.Persistence.Repositories;
using WhaleWire.Tests.Common.Builders;
using WhaleWire.Tests.Common.Fixtures;

namespace WhaleWire.Tests.Unit.Repositories;

public sealed class LeaseRepositoryTests : InMemoryDbContextFixture
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly LeaseRepository _repository;

    public LeaseRepositoryTests()
    {
        _repository = new LeaseRepository(Context, _timeProvider);
    }

    [Fact]
    public async Task TryAcquireLease_NewLease_ReturnsTrue()
    {
        // Arrange
        var (leaseKey, ownerId) = ObjectMother.Leases.Default();

        // Act
        var acquired = await _repository.TryAcquireLeaseAsync(
            leaseKey, ownerId, TimeSpan.FromMinutes(5));

        // Assert
        acquired.Should().BeTrue();
        Context.AddressLeases.Should().ContainSingle()
            .Which.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public async Task TryAcquireLease_SameOwnerTwice_RenewsLease()
    {
        // Arrange
        var (leaseKey, ownerId) = ObjectMother.Leases.Default();

        await _repository.TryAcquireLeaseAsync(leaseKey, ownerId, TimeSpan.FromSeconds(30));
        var firstExpiry = Context.AddressLeases.First().ExpiresAt;

        // Act - Same owner tries again (should extend)
        var acquired = await _repository.TryAcquireLeaseAsync(
            leaseKey, ownerId, TimeSpan.FromMinutes(10));

        // Assert
        acquired.Should().BeTrue();
        Context.AddressLeases.Should().ContainSingle();

        var currentExpiry = Context.AddressLeases.First().ExpiresAt;
        currentExpiry.Should().BeAfter(firstExpiry);
    }

    [Fact]
    public async Task TryAcquireLease_DifferentOwner_ActiveLease_ReturnsFalse()
    {
        // Arrange
        var (leaseKey, owner1) = ObjectMother.Leases.Default();
        const string owner2 = "worker-2";

        await _repository.TryAcquireLeaseAsync(leaseKey, owner1, TimeSpan.FromMinutes(5));

        // Act - Different owner tries to acquire
        var acquired = await _repository.TryAcquireLeaseAsync(
            leaseKey, owner2, TimeSpan.FromMinutes(5));

        // Assert
        acquired.Should().BeFalse();
        Context.AddressLeases.Should().ContainSingle()
            .Which.OwnerId.Should().Be(owner1); // Original owner unchanged
    }

    [Fact]
    public async Task TryAcquireLease_ExpiredLease_CanBeReacquired()
    {
        // Arrange
        var (leaseKey, owner1) = ObjectMother.Leases.Default();
        const string owner2 = "worker-2";
        var duration = TimeSpan.FromMinutes(1);

        await _repository.TryAcquireLeaseAsync(leaseKey, owner1, duration);

        // Act
        var timeBeyondExpiration = duration * 2;
        _timeProvider.Advance(timeBeyondExpiration);

        var acquired = await _repository.TryAcquireLeaseAsync(leaseKey, owner2, duration);

        // Assert
        acquired.Should().BeTrue();
    }

    [Fact]
    public async Task RenewLease_ValidOwner_ReturnsTrue()
    {
        // Arrange
        var (leaseKey, ownerId) = ObjectMother.Leases.Default();

        await _repository.TryAcquireLeaseAsync(leaseKey, ownerId, TimeSpan.FromSeconds(10));
        var originalExpiry = Context.AddressLeases.First().ExpiresAt;

        await Task.Delay(100); // Small delay to ensure time difference

        // Act
        var renewed = await _repository.RenewLeaseAsync(
            leaseKey, ownerId, TimeSpan.FromMinutes(5));

        // Assert
        renewed.Should().BeTrue();
        var newExpiry = Context.AddressLeases.First().ExpiresAt;
        newExpiry.Should().BeAfter(originalExpiry);
    }

    [Fact]
    public async Task RenewLease_WrongOwner_ReturnsFalse()
    {
        // Arrange
        var (leaseKey, owner1) = ObjectMother.Leases.Default();
        const string owner2 = "worker-2";

        await _repository.TryAcquireLeaseAsync(leaseKey, owner1, TimeSpan.FromMinutes(5));

        // Act - Different owner tries to renew
        var renewed = await _repository.RenewLeaseAsync(
            leaseKey, owner2, TimeSpan.FromMinutes(5));

        // Assert
        renewed.Should().BeFalse();
    }

    [Fact]
    public async Task RenewLease_NonExistentLease_ReturnsFalse()
    {
        // Arrange
        var (leaseKey, ownerId) = ObjectMother.Leases.Default();

        // Act - Try to renew lease that doesn't exist
        var renewed = await _repository.RenewLeaseAsync(
            leaseKey, ownerId, TimeSpan.FromMinutes(5));

        // Assert
        renewed.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseLease_ValidOwner_ReturnsTrue()
    {
        // Arrange
        var (leaseKey, ownerId) = ObjectMother.Leases.Default();

        await _repository.TryAcquireLeaseAsync(leaseKey, ownerId, TimeSpan.FromMinutes(5));

        // Act
        var released = await _repository.ReleaseLeaseAsync(leaseKey, ownerId);

        // Assert
        released.Should().BeTrue();
        Context.AddressLeases.Should().BeEmpty();
    }

    [Fact]
    public async Task ReleaseLease_WrongOwner_ReturnsFalse()
    {
        // Arrange
        var (leaseKey, owner1) = ObjectMother.Leases.Default();
        const string owner2 = "worker-2";

        await _repository.TryAcquireLeaseAsync(leaseKey, owner1, TimeSpan.FromMinutes(5));

        // Act - Different owner tries to release
        var released = await _repository.ReleaseLeaseAsync(leaseKey, owner2);

        // Assert
        released.Should().BeFalse();
        Context.AddressLeases.Should().ContainSingle(); // Lease still exists
    }

    [Fact]
    public async Task ReleaseLease_NonExistentLease_ReturnsFalse()
    {
        // Arrange
        var (leaseKey, ownerId) = ObjectMother.Leases.Default();

        // Act
        var released = await _repository.ReleaseLeaseAsync(leaseKey, ownerId);

        // Assert
        released.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleLeasesForDifferentKeys_CoexistIndependently()
    {
        // Arrange
        var lease1 = ("ton:address1", "worker-1");
        var lease2 = ("ton:address2", "worker-2");

        // Act
        var acquired1 = await _repository.TryAcquireLeaseAsync(
            lease1.Item1, lease1.Item2, TimeSpan.FromMinutes(5));
        var acquired2 = await _repository.TryAcquireLeaseAsync(
            lease2.Item1, lease2.Item2, TimeSpan.FromMinutes(5));

        // Assert
        acquired1.Should().BeTrue();
        acquired2.Should().BeTrue();
        Context.AddressLeases.Should().HaveCount(2);
    }
}