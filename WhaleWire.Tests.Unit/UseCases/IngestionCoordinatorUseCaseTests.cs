using FluentAssertions;
using NSubstitute;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;

namespace WhaleWire.Tests.Unit.UseCases;

public sealed class IngestionCoordinatorUseCaseTests
{
    private const string Chain = "ton";
    private const string Provider = "tonapi";
    private const string Address1 = "addr1";
    private const string Address2 = "addr2";
    private const string Address3 = "addr3";

    private readonly IIngestorUseCase _ingestorUseCase = Substitute.For<IIngestorUseCase>();
    private readonly IMonitoredAddressRepository _monitoredAddressRepo = Substitute.For<IMonitoredAddressRepository>();
    private readonly IBlockchainClient _blockchainClient = Substitute.For<IBlockchainClient>();
    private readonly IngestionCoordinatorUseCase _useCase;

    public IngestionCoordinatorUseCaseTests()
    {
        _blockchainClient.Chain.Returns(Chain);
        _blockchainClient.Provider.Returns(Provider);
        _useCase = new IngestionCoordinatorUseCase(
            _ingestorUseCase, _monitoredAddressRepo, _blockchainClient);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoAddresses_ReturnsZeroResult()
    {
        _monitoredAddressRepo.GetActiveAddressesAsync(Chain, Provider, Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var result = await _useCase.ExecuteAsync();

        result.AddressesProcessed.Should().Be(0);
        result.TotalEventsPublished.Should().Be(0);
        await _ingestorUseCase.DidNotReceive().ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithAddresses_ProcessesEachAddress()
    {
        var addresses = new List<string> { Address1, Address2, Address3 };
        _monitoredAddressRepo.GetActiveAddressesAsync(Chain, Provider, Arg.Any<CancellationToken>())
            .Returns(addresses);
        
        _ingestorUseCase.ExecuteAsync(Address1, Arg.Any<CancellationToken>()).Returns(5);
        _ingestorUseCase.ExecuteAsync(Address2, Arg.Any<CancellationToken>()).Returns(3);
        _ingestorUseCase.ExecuteAsync(Address3, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _useCase.ExecuteAsync();

        result.AddressesProcessed.Should().Be(3);
        result.TotalEventsPublished.Should().Be(8);
        result.Results.Should().HaveCount(3);
        result.Results.Should().AllSatisfy(r => r.Error.Should().BeNull());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAddressFails_ContinuesWithOthers()
    {
        var addresses = new List<string> { Address1, Address2, Address3 };
        _monitoredAddressRepo.GetActiveAddressesAsync(Chain, Provider, Arg.Any<CancellationToken>())
            .Returns(addresses);
        
        _ingestorUseCase.ExecuteAsync(Address1, Arg.Any<CancellationToken>()).Returns(5);
        _ingestorUseCase.ExecuteAsync(Address2, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new Exception("API error")));
        _ingestorUseCase.ExecuteAsync(Address3, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _useCase.ExecuteAsync();

        result.AddressesProcessed.Should().Be(3);
        result.TotalEventsPublished.Should().Be(8);
        result.Results[0].Error.Should().BeNull();
        result.Results[1].Error.Should().Contain("API error");
        result.Results[2].Error.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_UsesBlockchainClientMetadata()
    {
        var addresses = new List<string> { Address1 };
        _monitoredAddressRepo.GetActiveAddressesAsync(Chain, Provider, Arg.Any<CancellationToken>())
            .Returns(addresses);

        await _useCase.ExecuteAsync();

        await _monitoredAddressRepo.Received(1).GetActiveAddressesAsync(
            Chain, Provider, Arg.Any<CancellationToken>());
    }
}
