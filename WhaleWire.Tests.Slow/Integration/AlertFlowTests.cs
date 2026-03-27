using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Alerts;
using WhaleWire.Domain;
using WhaleWire.Messages;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

public sealed class AlertFlowTests(WhaleWireIntegrationFixture fixture) 
    : IClassFixture<WhaleWireIntegrationFixture>
{
    [Fact]
    public async Task E2E_LargeTransfer_TriggersAlert()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var alertEvaluator = scope.ServiceProvider.GetRequiredService<IAlertEvaluator>();
        
        var largeTransferEvent = CreateLargeTransferEvent();

        // Act
        var result = await alertEvaluator.EvaluateAsync(largeTransferEvent);

        // Assert
        result.Alerts.Should().NotBeEmpty();
        result.Alerts[0].AssetId.Should().Be("TON");
        result.Alerts[0].Amount.Should().BeGreaterThan(0);
    }

    private static BlockchainEvent CreateLargeTransferEvent()
    {
        return new BlockchainEvent
        {
            EventId = "test-alert-001",
            Chain = "ton",
            Provider = "tonapi",
            Address = "0:WHALE123",
            Cursor = new Cursor(1000, "hash1000"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """
                      {
                          "in_msg": {
                              "source": "0:WHALE123",
                              "destination": "0:EXCHANGE456",
                              "value": "10000000000000"
                          }
                      }
                      """
        };
    }
}
