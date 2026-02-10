using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WhaleWire.Application.Alerts;
using WhaleWire.Infrastructure.Notifications.Notifiers;

namespace WhaleWire.Tests.Unit.Notifiers;

public sealed class ConsoleAlertNotifierTests
{
    private readonly Mock<ILogger<ConsoleAlertNotifier>> _loggerMock = new();
    private readonly ConsoleAlertNotifier _notifier;

    public ConsoleAlertNotifierTests()
    {
        _notifier = new ConsoleAlertNotifier(_loggerMock.Object);
    }

    [Fact]
    public async Task NotifyAsync_LogsAlertWithWarningLevel()
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

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WHALE ALERT")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
