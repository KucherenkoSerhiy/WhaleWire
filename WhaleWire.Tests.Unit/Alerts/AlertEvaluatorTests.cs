using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WhaleWire.Application.Alerts;
using WhaleWire.Domain;
using WhaleWire.Messages;

namespace WhaleWire.Tests.Unit.Alerts;

public sealed class AlertEvaluatorTests
{
    private const string TestAddress = "0:TEST123";
    private const string TestEventId = "test-001";
    
    private readonly AlertEvaluator _evaluator = new(
        NullLogger<AlertEvaluator>.Instance,
        NullWhaleDecisionAuditLogger.Instance);

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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].AssetId.Should().Be("TON");
        result.Alerts[0].WalletAddress.Should().Be(TestAddress);
        result.Alerts[0].Direction.Should().Be("IN");
        result.Alerts[0].Amount.Should().Be(150m);
        result.Alerts[0].Message.Should().Contain("Received from");
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].AssetId.Should().Be("TON");
        result.Alerts[0].Direction.Should().Be("OUT");
        result.Alerts[0].Amount.Should().Be(250m);
        result.Alerts[0].Message.Should().Contain("Sent to");
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().BeEmpty();
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].Amount.Should().Be(200m);
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().HaveCount(2);
        result.Alerts[0].Amount.Should().Be(150m);
        result.Alerts[1].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task EvaluateAsync_InvalidJson_ReturnsEmpty()
    {
        // Arrange
        var evt = CreateEvent("{ invalid json }");

        // Act
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().BeEmpty();
        result.JsonParseFailed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_InvalidJson_LogsCorrelationId()
    {
        var loggerMock = new Mock<ILogger<AlertEvaluator>>();
        var evaluator = new AlertEvaluator(loggerMock.Object, NullWhaleDecisionAuditLogger.Instance);
        var evt = CreateEvent("{ invalid json }", correlationId: "parse-fail-correlation-id");

        await evaluator.EvaluateAsync(evt);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("parse-fail-correlation-id")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_InvalidJson_WritesWhaleDecisionAudit()
    {
        var auditMock = new Mock<IWhaleDecisionAuditLogger>();
        var evaluator = new AlertEvaluator(NullLogger<AlertEvaluator>.Instance, auditMock.Object);
        var evt = CreateEvent("{ invalid json }", correlationId: "audit-corr");

        await evaluator.EvaluateAsync(evt);

        auditMock.Verify(
            x => x.Log(It.Is<WhaleDecisionRecord>(r =>
                r.Outcome == "failed"
                && r.ReasonCode == WhaleDecisionReasonCodes.RawJsonParseError
                && r.EventId == TestEventId
                && r.CorrelationId == "audit-corr")),
            Times.Once);
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().BeEmpty();
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_SourceDestinationAsObject_TonApiFormat_ReturnsAlert()
    {
        // TonAPI may return source/destination as {"workchain_id": 0, "address": "EQD..."}
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "source": {"workchain_id": 0, "address": "EQD0vdQ_NuRqV8Zg_9Khzd8rE2bJQpNqaV1W3SHK8xTpq"},
                    "destination": {"workchain_id": 0, "address": "EQCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id"},
                    "value": "150000000000"
                }
            }
            """);

        var result = await _evaluator.EvaluateAsync(evt);

        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].Amount.Should().Be(150m);
        result.Alerts[0].Message.Should().Contain("0:EQD0vdQ_");
    }

    [Fact]
    public async Task EvaluateAsync_ValueAsNumber_TonApiFormat_ReturnsAlert()
    {
        // TonAPI may return value as number instead of string
        var evt = CreateEvent("""
            {
                "in_msg": {
                    "value": 250000000000
                }
            }
            """);

        var result = await _evaluator.EvaluateAsync(evt);

        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].Amount.Should().Be(250m);
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
        var result = await _evaluator.EvaluateAsync(evt);

        // Assert
        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].Amount.Should().Be(100m);
    }

    private static BlockchainEvent CreateEvent(string rawJson, string? correlationId = null)
    {
        return new BlockchainEvent
        {
            EventId = TestEventId,
            Chain = "ton",
            Provider = "tonapi",
            Address = TestAddress,
            CorrelationId = correlationId,
            Cursor = new Cursor(1000, "hash"),
            OccurredAt = DateTime.UtcNow,
            RawJson = rawJson
        };
    }
}
