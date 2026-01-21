using FluentAssertions;
using NSubstitute;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;
using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Tests.Unit.UseCases;

public sealed class IngestorUseCaseTests
{
    private const string Chain = "ton";
    private const string Provider = "tonapi";
    private const string Address = "EQTestAddress";
    private const string LeaseKey = "ton:tonapi:EQTestAddress";
    private const string OwnerId = "ingestor";

    private readonly IBlockchainClient _blockchainClient = Substitute.For<IBlockchainClient>();
    private readonly ILeaseRepository _leaseRepository = Substitute.For<ILeaseRepository>();
    private readonly ICheckpointRepository _checkpointRepository = Substitute.For<ICheckpointRepository>();
    private readonly IMessagePublisher _messagePublisher = Substitute.For<IMessagePublisher>();
    private readonly IngestorUseCase _useCase;

    public IngestorUseCaseTests()
    {
        _blockchainClient.Chain.Returns(Chain);
        _blockchainClient.Provider.Returns(Provider);
        _useCase = new IngestorUseCase(
            _blockchainClient, _leaseRepository, _checkpointRepository, _messagePublisher);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLeaseNotAcquired_ReturnsZero()
    {
        _leaseRepository.TryAcquireLeaseAsync(LeaseKey, OwnerId, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var count = await _useCase.ExecuteAsync(Address);

        count.Should().Be(0);
        await _blockchainClient.DidNotReceive().GetEventsAsync(Arg.Any<string>(), Arg.Any<Cursor>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoCheckpoint_FetchesFromBeginning()
    {
        _leaseRepository.TryAcquireLeaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _checkpointRepository.GetCheckpointAsync(Chain, Address, Provider, Arg.Any<CancellationToken>())
            .Returns((CheckpointData?)null);
        _blockchainClient.GetEventsAsync(Address, null, 100, Arg.Any<CancellationToken>())
            .Returns(new List<BlockchainEvent>());

        await _useCase.ExecuteAsync(Address);

        await _blockchainClient.Received(1).GetEventsAsync(Address, null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenHasCheckpoint_FetchesAfterCursor()
    {
        var checkpointData = new CheckpointData(100, "hash-100", DateTime.UtcNow);
        _leaseRepository.TryAcquireLeaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _checkpointRepository.GetCheckpointAsync(Chain, Address, Provider, Arg.Any<CancellationToken>())
            .Returns(checkpointData);
        _blockchainClient.GetEventsAsync(Address, Arg.Any<Cursor>(), 100, Arg.Any<CancellationToken>())
            .Returns(new List<BlockchainEvent>());

        await _useCase.ExecuteAsync(Address);

        await _blockchainClient.Received(1).GetEventsAsync(
            Address,
            Arg.Is<Cursor>(c => c.Primary == 100 && c.Secondary == "hash-100"),
            100,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithEvents_PublishesEachEvent()
    {
        var events = new List<BlockchainEvent>
        {
            CreateEvent("event1", 101, "hash1"),
            CreateEvent("event2", 102, "hash2")
        };
        
        _leaseRepository.TryAcquireLeaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _checkpointRepository.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CheckpointData?)null);
        _blockchainClient.GetEventsAsync(Arg.Any<string>(), Arg.Any<Cursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(events);

        var count = await _useCase.ExecuteAsync(Address);

        count.Should().Be(2);
        await _messagePublisher.Received(2).PublishAsync(Arg.Any<BlockchainEvent>(), Arg.Any<CancellationToken>());
        await _messagePublisher.Received(1).PublishAsync(events[0], Arg.Any<CancellationToken>());
        await _messagePublisher.Received(1).PublishAsync(events[1], Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AlwaysReleasesLease_EvenOnError()
    {
        _leaseRepository.TryAcquireLeaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _blockchainClient.GetEventsAsync(Arg.Any<string>(), Arg.Any<Cursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<BlockchainEvent>>(new Exception("API failure")));

        await Assert.ThrowsAsync<Exception>(() => _useCase.ExecuteAsync(Address));

        await _leaseRepository.Received(1).ReleaseLeaseAsync(LeaseKey, OwnerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoEvents_ReturnsZero()
    {
        _leaseRepository.TryAcquireLeaseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _checkpointRepository.GetCheckpointAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CheckpointData?)null);
        _blockchainClient.GetEventsAsync(Arg.Any<string>(), Arg.Any<Cursor?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<BlockchainEvent>());

        var count = await _useCase.ExecuteAsync(Address);

        count.Should().Be(0);
        await _messagePublisher.DidNotReceive().PublishAsync(Arg.Any<BlockchainEvent>(), Arg.Any<CancellationToken>());
    }

    private static BlockchainEvent CreateEvent(string eventId, long lt, string hash)
    {
        return new BlockchainEvent
        {
            EventId = eventId,
            Chain = Chain,
            Provider = Provider,
            Address = Address,
            Cursor = new Cursor(lt, hash),
            OccurredAt = DateTime.UtcNow,
            RawJson = "{}"
        };
    }
}
