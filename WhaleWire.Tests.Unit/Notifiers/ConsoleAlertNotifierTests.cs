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
    private readonly Mock<IWhaleDecisionAuditLogger> _whaleAuditMock = new();
    private readonly ConsoleAlertNotifier _notifier;

    public ConsoleAlertNotifierTests()
    {
        _correlationIdAccessorMock.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        _notifier = new ConsoleAlertNotifier(
            _loggerMock.Object,
            _correlationIdAccessorMock.Object,
            new NullWhaleWireMetrics(),
            _whaleAuditMock.Object);
    }

    [Fact]
    public async Task NotifyAsync_LogsHumanWhaleLineAndAuditSent()
    {
        var alert = new Alert(
            AssetId: "TON",
            WalletAddress: "0:1234567890ABCDEF",
            Amount: 150m,
            Direction: "IN",
            Message: "Large transfer detected",
            EventId: "evt-1",
            Chain: "ton",
            Address: "0:1234567890ABCDEF",
            Provider: "tonapi",
            CorrelationId: "test-correlation-id");

        await _notifier.NotifyAsync(alert);

        _whaleAuditMock.Verify(
            x => x.Log(It.Is<WhaleDecisionRecord>(r =>
                r.Outcome == "sent"
                && r.Channel == "console"
                && r.EventId == "evt-1"
                && r.CorrelationId == "test-correlation-id"
                && r.AssetId == "TON"
                && r.Direction == "IN"
                && r.Amount == 150m)),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyAsync_CompletesSuccessfully()
    {
        var alert = new Alert(
            AssetId: "TON",
            WalletAddress: "0:TEST",
            Amount: 100m,
            Direction: "OUT",
            Message: "Test");

        var act = async () => await _notifier.NotifyAsync(alert);

        await act.Should().NotThrowAsync();
    }
}
