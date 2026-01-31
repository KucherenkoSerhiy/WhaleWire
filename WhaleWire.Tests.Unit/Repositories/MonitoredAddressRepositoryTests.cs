using FluentAssertions;
using WhaleWire.Infrastructure.Persistence.Repositories;
using WhaleWire.Tests.Common.Fixtures;

namespace WhaleWire.Tests.Unit.Repositories;

public sealed class MonitoredAddressRepositoryTests : InMemoryDbContextFixture
{
    private readonly MonitoredAddressRepository _repository;

    public MonitoredAddressRepositoryTests()
    {
        _repository = new MonitoredAddressRepository(Context);
    }

    [Fact]
    public async Task GetActiveAddresses_WhenEmpty_ReturnsEmptyList()
    {
        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");

        addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAddresses_WhenExists_ReturnsAddresses()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");
        await _repository.UpsertAddressAsync("ton", "addr2", "tonapi", "2000");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");

        addresses.Should().HaveCount(2);
        addresses.Should().Contain("addr1");
        addresses.Should().Contain("addr2");
    }

    [Fact]
    public async Task GetActiveAddresses_OrdersByBalanceDescending()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");
        await _repository.UpsertAddressAsync("ton", "addr2", "tonapi", "5000");
        await _repository.UpsertAddressAsync("ton", "addr3", "tonapi", "3000");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");

        addresses.Should().HaveCount(3);
        addresses[0].Should().Be("addr2"); // 5000
        addresses[1].Should().Be("addr3"); // 3000
        addresses[2].Should().Be("addr1"); // 1000
    }

    [Fact]
    public async Task GetActiveAddresses_OnlyReturnsActive()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");
        await _repository.UpsertAddressAsync("ton", "addr2", "tonapi", "2000");
        await _repository.DeactivateAddressAsync("ton", "addr1", "tonapi");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");

        addresses.Should().ContainSingle();
        addresses.Should().Contain("addr2");
    }

    [Fact]
    public async Task UpsertAddress_WhenNew_CreatesAddress()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");
        addresses.Should().ContainSingle();
        addresses[0].Should().Be("addr1");
    }

    [Fact]
    public async Task UpsertAddress_WhenExists_UpdatesBalance()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "2000");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");
        addresses.Should().ContainSingle();
        
        var address = Context.MonitoredAddresses.First(m => m.Address == "addr1");
        address.Balance.Should().Be("2000");
    }

    [Fact]
    public async Task UpsertAddress_WhenDeactivated_ReactivatesAndUpdates()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");
        await _repository.DeactivateAddressAsync("ton", "addr1", "tonapi");
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "3000");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");
        addresses.Should().ContainSingle();
        addresses[0].Should().Be("addr1");
        
        var address = Context.MonitoredAddresses.First(m => m.Address == "addr1");
        address.Balance.Should().Be("3000");
        address.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAddress_WhenExists_SetsInactive()
    {
        await _repository.UpsertAddressAsync("ton", "addr1", "tonapi", "1000");
        
        await _repository.DeactivateAddressAsync("ton", "addr1", "tonapi");

        var addresses = await _repository.GetActiveAddressesAsync("ton", "tonapi");
        addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task DeactivateAddress_WhenNotExists_DoesNotThrow()
    {
        var act = async () => await _repository.DeactivateAddressAsync("ton", "nonexistent", "tonapi");

        await act.Should().NotThrowAsync();
    }
}
