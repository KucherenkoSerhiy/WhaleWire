using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WhaleWire.Application.Alerts;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Configuration;
using WhaleWire.Domain;
using WhaleWire.Handlers;
using WhaleWire.Messages;

namespace WhaleWire.Tests.Unit.Handlers;

public sealed class BlockchainEventHandlerTests
{
    private const string TestChain = "ton";
    private const string TestProvider = "tonapi";
    private const string TestAddress = "0:TEST123";
    private const string TestEventId = "test-event-001";

    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<ICheckpointRepository> _checkpointRepoMock = new();
    private readonly Mock<IAlertEvaluator> _alertEvaluatorMock = new();
    private readonly Mock<IAlertNotifier> _alertNotifierMock = new();
    private readonly BlockchainEventHandler _handler;

    public BlockchainEventHandlerTests()
    {
        var options = Options.Create(new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 5,
            DurationOfBreakMinutes = 1
        });

        _handler = new BlockchainEventHandler(
            _eventRepoMock.Object,
            _checkpointRepoMock.Object,
            _alertEvaluatorMock.Object,
            _alertNotifierMock.Object,
            new NullWhaleWireMetrics(),
            NullLogger<BlockchainEventHandler>.Instance,
            options);
    }

    [Fact]
    public async Task HandleAsync_NewEvent_InsertsEventAndUpdatesCheckpoint()
    {
        // Arrange
        var evt = CreateTestEvent();
        _eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _alertEvaluatorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<BlockchainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        // Act
        await _handler.HandleAsync(evt);

        // Assert
        _eventRepoMock.Verify(
            x => x.UpsertEventIdempotentAsync(
                TestEventId,
                TestChain,
                TestAddress,
                evt.Cursor.Primary,
                evt.Cursor.Secondary,
                evt.OccurredAt,
                evt.RawJson,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _checkpointRepoMock.Verify(
            x => x.UpdateCheckpointMonotonicAsync(
                TestChain,
                TestAddress,
                TestProvider,
                evt.Cursor.Primary,
                evt.Cursor.Secondary,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_SkipsCheckpointUpdate()
    {
        // Arrange
        var evt = CreateTestEvent();
        _eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(evt);

        // Assert
        _checkpointRepoMock.Verify(
            x => x.UpdateCheckpointMonotonicAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewEvent_EvaluatesAlerts()
    {
        // Arrange
        var evt = CreateTestEvent();
        _eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _alertEvaluatorMock
            .Setup(x => x.EvaluateAsync(evt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        // Act
        await _handler.HandleAsync(evt);

        // Assert
        _alertEvaluatorMock.Verify(
            x => x.EvaluateAsync(evt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NewEventWithAlerts_NotifiesAllAlerts()
    {
        // Arrange
        var evt = CreateTestEvent();
        var alerts = new List<Alert>
        {
            new("TON", TestAddress, 150m, "IN", "Test alert 1"),
            new("TON", TestAddress, 200m, "OUT", "Test alert 2")
        };

        _eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _alertEvaluatorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<BlockchainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        // Act
        await _handler.HandleAsync(evt);

        // Assert
        _alertNotifierMock.Verify(
            x => x.NotifyAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_DoesNotEvaluateAlerts()
    {
        // Arrange
        var evt = CreateTestEvent();
        _eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.HandleAsync(evt);

        // Assert
        _alertEvaluatorMock.Verify(
            x => x.EvaluateAsync(It.IsAny<BlockchainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _alertNotifierMock.Verify(
            x => x.NotifyAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NewEventWithNoAlerts_DoesNotNotify()
    {
        // Arrange
        var evt = CreateTestEvent();
        _eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _alertEvaluatorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<BlockchainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Alert>());

        // Act
        await _handler.HandleAsync(evt);

        // Assert
        _alertNotifierMock.Verify(
            x => x.NotifyAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static BlockchainEvent CreateTestEvent()
    {
        return new BlockchainEvent
        {
            EventId = TestEventId,
            Chain = TestChain,
            Provider = TestProvider,
            Address = TestAddress,
            Cursor = new Cursor(1000, "hash1000"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """{"test": "data"}"""
        };
    }
}
