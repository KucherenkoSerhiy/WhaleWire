using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WhaleWire.Application.Alerts;
using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Tests.Unit.Alerts;

public sealed class AlertEvaluatorTests
{
    private const string TestAddress = "0:TEST123";
    private const string TestEventId = "test-001";
    
    private readonly AlertEvaluator _evaluator = new(NullLogger<AlertEvaluator>.Instance);

    [Fact]
    public async Task EvaluateAsync_LargeIncomingTransfer_ReturnsAlert()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "source": "0:SOURCE",
                    "destination": "0:DEST",
                    "value": "150000000000"
                }
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().HaveCount(1);
        alerts[0].AssetId.Should().Be("TON");
        alerts[0].WalletAddress.Should().Be(TestAddress);
        alerts[0].Direction.Should().Be("IN");
        alerts[0].Amount.Should().Be(150m);
        alerts[0].Message.Should().Contain("Received from");
    }

    [Fact]
    public async Task EvaluateAsync_LargeOutgoingTransfer_ReturnsAlert()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "out_msgs": [
                    {
                        "source": "0:SOURCE",
                        "destination": "0:DEST",
                        "value": "250000000000"
                    }
                ]
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().HaveCount(1);
        alerts[0].AssetId.Should().Be("TON");
        alerts[0].Direction.Should().Be("OUT");
        alerts[0].Amount.Should().Be(250m);
        alerts[0].Message.Should().Contain("Sent to");
    }

    [Fact]
    public async Task EvaluateAsync_BelowThreshold_ReturnsEmpty()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "value": "50000000000"
                }
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_TonApiFormat_WithTransactionsArray_ReturnsAlert()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "transactions": [
                    {
                        "in_msg": {
                            "value": "200000000000"
                        }
                    }
                ]
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().HaveCount(1);
        alerts[0].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleOutgoingMessages_ReturnsMultipleAlerts()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "out_msgs": [
                    {
                        "value": "150000000000"
                    },
                    {
                        "value": "200000000000"
                    }
                ]
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().HaveCount(2);
        alerts[0].Amount.Should().Be(150m);
        alerts[1].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task EvaluateAsync_InvalidJson_ReturnsEmpty()
    {
        // Arrange
        var evt = CreateEvent("{ invalid json }");

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_NoValueField_ReturnsEmpty()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "source": "0:SOURCE"
                }
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_EmptyValue_ReturnsEmpty()
    {
        // Arrange
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "value": ""
                }
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ExactThreshold_ReturnsAlert()
    {
        // Arrange - 100 TON exactly (at threshold, should trigger)
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "value": "100000000000"
                }
            }
            """);

        // Act
        var alerts = await _evaluator.EvaluateAsync(evt);

        // Assert
        alerts.Should().HaveCount(1);
        alerts[0].Amount.Should().Be(100m);
    }

    private static BlockchainEvent CreateEvent(string rawJson)
    {
        return new BlockchainEvent
        {
            EventId = TestEventId,
            Chain = "ton",
            Provider = "tonapi",
            Address = TestAddress,
            Cursor = new Cursor(1000, "hash"),
            OccurredAt = DateTime.UtcNow,
            RawJson = rawJson
        };
    }
}
