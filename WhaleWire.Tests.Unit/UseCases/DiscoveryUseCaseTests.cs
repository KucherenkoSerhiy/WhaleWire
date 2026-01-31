using FluentAssertions;
using NSubstitute;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;

namespace WhaleWire.Tests.Unit.UseCases;

public sealed class DiscoveryUseCaseTests
{
    private const string Chain = "ton";
    private const string Provider = "tonapi";
    private const int Limit = 100;

    private readonly ITopAccountsClient _topAccountsClient = Substitute.For<ITopAccountsClient>();
    private readonly IMonitoredAddressRepository _monitoredAddressRepo = Substitute.For<IMonitoredAddressRepository>();
    private readonly IBlockchainClient _blockchainClient = Substitute.For<IBlockchainClient>();
    private readonly DiscoveryUseCase _useCase;

    public DiscoveryUseCaseTests()
    {
        _blockchainClient.Chain.Returns(Chain);
        _blockchainClient.Provider.Returns(Provider);
        _useCase = new DiscoveryUseCase(_topAccountsClient, _monitoredAddressRepo, _blockchainClient);
    }

    [Fact]
    public async Task ExecuteAsync_WithTopAccounts_UpsertAllAddresses()
    {
        var accounts = new List<TopAccount>
        {
            new("addr1", 5000),
            new("addr2", 3000),
            new("addr3", 1000)
        };
        _topAccountsClient.GetTopAccountsByBalanceAsync(Limit, Arg.Any<CancellationToken>())
            .Returns(accounts);

        var count = await _useCase.ExecuteAsync(Limit);

        count.Should().Be(3);
        await _monitoredAddressRepo.Received(1).UpsertAddressAsync(
            Chain, "addr1", Provider, "5000", Arg.Any<CancellationToken>());
        await _monitoredAddressRepo.Received(1).UpsertAddressAsync(
            Chain, "addr2", Provider, "3000", Arg.Any<CancellationToken>());
        await _monitoredAddressRepo.Received(1).UpsertAddressAsync(
            Chain, "addr3", Provider, "1000", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoAccounts_ReturnsZero()
    {
        _topAccountsClient.GetTopAccountsByBalanceAsync(Limit, Arg.Any<CancellationToken>())
            .Returns(new List<TopAccount>());

        var count = await _useCase.ExecuteAsync(Limit);

        count.Should().Be(0);
        await _monitoredAddressRepo.DidNotReceive().UpsertAddressAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_UsesBlockchainClientMetadata()
    {
        var accounts = new List<TopAccount> { new("addr1", 1000) };
        _topAccountsClient.GetTopAccountsByBalanceAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(accounts);

        await _useCase.ExecuteAsync(Limit);

        await _monitoredAddressRepo.Received(1).UpsertAddressAsync(
            Chain, "addr1", Provider, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
