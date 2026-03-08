using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polly.CircuitBreaker;
using Prometheus;
using WhaleWire.Application.CorrelationId;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.Persistence;
using WhaleWire.Configuration;
using WhaleWire.Domain;
using WhaleWire.Handlers;
using WhaleWire.Messages;
using WhaleWire.Metrics;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// Integration tests for circuit breaker metric.
/// Verifies the metric is recorded correctly in real flows.
/// </summary>
public sealed class CircuitBreakerMetricTests
{
    [Fact]
    public async Task E2E_SuccessfulHandle_RecordsCircuitBreakerStateClosed()
    {
        // Arrange - real infrastructure, real WhaleWireMetrics
        var fixture = new WhaleWireMetricsIntegrationFixture();
        await fixture.InitializeAsync();
        try
        {
            using var scope = fixture.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();

            var evt = new BlockchainEvent
            {
                EventId = $"closed-{Guid.NewGuid()}",
                Chain = "ton",
                Provider = "tonapi",
                Address = "0:TEST_ADDRESS",
                Cursor = new Cursor(9000, "hash9000"),
                OccurredAt = DateTime.UtcNow,
                RawJson = """{"in_msg": {"value": "50000000000"}}"""
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert - metric appears in Prometheus export with state=0 (closed)
            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().Contain("whalewire_circuit_breaker_state");
            output.Should().Contain("whalewire_circuit_breaker_state 0");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task CircuitOpensAfterFiveFailures_MetricShowsOpen()
    {
        // Arrange - mock repository that throws, real WhaleWireMetrics + real Polly
        var eventRepoMock = new Mock<IEventRepository>();
        eventRepoMock
            .Setup(x => x.UpsertEventIdempotentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

        var checkpointRepoMock = new Mock<ICheckpointRepository>();
        var alertEvaluatorMock = new Mock<WhaleWire.Application.Alerts.IAlertEvaluator>();
        var alertNotifierMock = new Mock<WhaleWire.Application.Alerts.IAlertNotifier>();
        var correlationIdAccessorMock = new Mock<ICorrelationIdAccessor>();
        correlationIdAccessorMock.Setup(x => x.CorrelationId).Returns("circuit-test");

        var options = Options.Create(new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 5,
            DurationOfBreakMinutes = 1
        });

        var metrics = new WhaleWireMetrics();
        var handler = new BlockchainEventHandler(
            eventRepoMock.Object,
            checkpointRepoMock.Object,
            alertEvaluatorMock.Object,
            alertNotifierMock.Object,
            correlationIdAccessorMock.Object,
            metrics,
            NullLogger<BlockchainEventHandler>.Instance,
            options);

        var evt = new BlockchainEvent
        {
            EventId = "open-test",
            Chain = "ton",
            Provider = "tonapi",
            Address = "0:ADDR",
            Cursor = new Cursor(1, "h1"),
            OccurredAt = DateTime.UtcNow,
            RawJson = "{}"
        };

        // Act - trigger 5 failures to open the circuit
        for (var i = 0; i < 5; i++)
        {
            await handler.HandleAsync(evt).Invoking(x => x).Should().ThrowAsync<InvalidOperationException>();
        }

        // 6th call - circuit is open, throws BrokenCircuitException
        await handler.HandleAsync(evt).Invoking(x => x).Should().ThrowAsync<BrokenCircuitException>();

        // Assert - metric shows open (2)
        await using var stream = new MemoryStream();
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("whalewire_circuit_breaker_state");
        output.Should().Contain("whalewire_circuit_breaker_state 2");
    }

    [Fact]
    public async Task E2E_EventLag_RecordedAfterSuccessfulHandle()
    {
        var fixture = new WhaleWireMetricsIntegrationFixture();
        await fixture.InitializeAsync();
        try
        {
            using var scope = fixture.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();
            var checkpointRepo = scope.ServiceProvider.GetRequiredService<ICheckpointRepository>();
            var metrics = scope.ServiceProvider.GetRequiredService<IWhaleWireMetrics>();

            var evt = new BlockchainEvent
            {
                EventId = $"lag-{Guid.NewGuid()}",
                Chain = "ton",
                Provider = "tonapi",
                Address = "0:LAG_ADDRESS",
                Cursor = new Cursor(9100, "hash9100"),
                OccurredAt = DateTime.UtcNow,
                RawJson = """{"in_msg": {"value": "10000000000"}}"""
            };

            await handler.HandleAsync(evt);

            var timestamps = await checkpointRepo.GetCheckpointTimestampsAsync();
            var now = DateTime.UtcNow;
            foreach (var ts in timestamps.Where(t => t.Address == "0:LAG_ADDRESS"))
            {
                var lagSeconds = (now - ts.UpdatedAt).TotalSeconds;
                metrics.RecordEventLag(ts.Chain, ts.Address, lagSeconds);
            }

            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().Contain("whalewire_event_lag_seconds");
            output.Should().Contain("address=\"0:LAG_ADDRESS\"");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
