using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using WhaleWire.Domain;
using WhaleWire.Handlers;
using WhaleWire.Messages;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// Integration tests for EventLagMetricsCollector and DlqMetricsCollector.
/// Verifies the background services run and update metrics correctly.
/// </summary>
public sealed class EventLagAndDlqCollectorTests
{
    [Fact]
    public async Task EventLagMetricsCollector_RunsAndUpdatesMetric()
    {
        var fixture = new WhaleWireMetricsHostFixture();
        await fixture.InitializeAsync();
        try
        {
            using var scope = fixture.Host.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();

            var evt = new BlockchainEvent
            {
                EventId = $"collector-lag-{Guid.NewGuid()}",
                Chain = "ton",
                Provider = "tonapi",
                Address = "0:COLLECTOR_LAG",
                Cursor = new Cursor(9200, "hash9200"),
                OccurredAt = DateTime.UtcNow,
                RawJson = """{"in_msg": {"value": "10000000000"}}"""
            };

            await handler.HandleAsync(evt);

            await Task.Delay(2500);

            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().Contain("whalewire_event_lag_seconds");
            output.Should().Contain("address=\"0:COLLECTOR_LAG\"");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task DlqMetricsCollector_RunsAndUpdatesMetric()
    {
        var fixture = new WhaleWireMetricsHostFixture();
        await fixture.InitializeAsync();
        try
        {
            await Task.Delay(2500);

            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().Contain("whalewire_dlq_messages_total");
            output.Should().Contain("queue=\"whalewire.blockchainevent.queue.dlq\"");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
