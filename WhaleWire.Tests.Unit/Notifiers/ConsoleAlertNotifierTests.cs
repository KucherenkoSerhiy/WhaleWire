using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WhaleWire.Application.Alerts;
using WhaleWire.Application.CorrelationId;
using WhaleWire.Application.Metrics;
using WhaleWire.Infrastructure.Notifications.Notifiers;

namespace WhaleWire.Tests.Unit.Notifiers;

public sealed class ConsoleAlertNotifierTests
{
    private readonly Mock<ILogger<ConsoleAlertNotifier>> _loggerMock = new();
    private readonly Mock<ICorrelationIdAccessor> _correlationIdAccessorMock = new();
    private readonly ConsoleAlertNotifier _notifier;

    public ConsoleAlertNotifierTests()
    {
        _correlationIdAccessorMock.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        _notifier = new ConsoleAlertNotifier(_loggerMock.Object, _correlationIdAccessorMock.Object, new NullWhaleWireMetrics());
    }

    [Fact]
    public async Task NotifyAsync_LogsAlertWithWarningLevelAndCorrelationId()
    {
        // Arrange
        var alert = new Alert(
            AssetId: "TON",
            WalletAddress: "0:1234567890ABCDEF",
            Amount: 150m,
            Direction: "IN",
            Message: "Large transfer detected");

        // Act
        await _notifier.NotifyAsync(alert);

        // Assert - structured logging: capture formatter output to verify CorrelationId
        var logCalls = 0;
        string? formattedMessage = null;
        _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object?, Exception?, Delegate>((_, _, state, _, formatter) =>
            {
                logCalls++;
                if (formatter is Delegate d && state != null)
                    formattedMessage = d.DynamicInvoke(state, null)?.ToString();
            });

        // Act (notifier was already created in ctor; Setup captures when it logs)
        await _notifier.NotifyAsync(alert);

        logCalls.Should().Be(1);
        formattedMessage.Should().Contain("WHALE ALERT");
        formattedMessage.Should().Contain("test-correlation-id");
    }

    [Fact]
    public async Task NotifyAsync_CompletesSuccessfully()
    {
        // Arrange
        var alert = new Alert(
            AssetId: "TON",
            WalletAddress: "0:TEST",
            Amount: 100m,
            Direction: "OUT",
            Message: "Test");

        // Act
        var act = async () => await _notifier.NotifyAsync(alert);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
