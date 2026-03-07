using System.Text;
using FluentAssertions;
using Prometheus;
using WhaleWire.Metrics;

namespace WhaleWire.Tests.Unit.Metrics;

public sealed class WhaleWireMetricsTests
{
    [Fact]
    public async Task RecordEventsIngested_DoesNotThrow_AndMetricAppearsInExport()
    {
        var metrics = new WhaleWireMetrics();
        var act = () => metrics.RecordEventsIngested(5, "ton");
        act.Should().NotThrow();

        await using var stream = new MemoryStream();
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("whalewire_events_ingested_total");
        output.Should().Contain("chain=\"ton\"");
    }

    [Fact]
    public async Task RecordAlertFired_DoesNotThrow_AndMetricAppearsInExport()
    {
        var metrics = new WhaleWireMetrics();
        var act = () => metrics.RecordAlertFired("TON", "IN");
        act.Should().NotThrow();

        await using var stream = new MemoryStream();
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("whalewire_alerts_fired_total");
        output.Should().Contain("asset=\"TON\"");
        output.Should().Contain("direction=\"IN\"");
    }

    [Fact]
    public async Task RecordCircuitBreakerState_DoesNotThrow_AndMetricAppearsInExport()
    {
        var metrics = new WhaleWireMetrics();
        var act = () => metrics.RecordCircuitBreakerState(0);
        act.Should().NotThrow();

        await using var stream = new MemoryStream();
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("whalewire_circuit_breaker_state");
    }

    [Fact]
    public async Task RecordEventLag_DoesNotThrow_AndMetricAppearsInExport()
    {
        var metrics = new WhaleWireMetrics();
        var act = () => metrics.RecordEventLag("ton", "0:ADDR", 120.5);
        act.Should().NotThrow();

        await using var stream = new MemoryStream();
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("whalewire_event_lag_seconds");
        output.Should().Contain("chain=\"ton\"");
        output.Should().Contain("address=\"0:ADDR\"");
    }

    [Fact]
    public async Task RecordDlqMessageCount_DoesNotThrow_AndMetricAppearsInExport()
    {
        var metrics = new WhaleWireMetrics();
        var act = () => metrics.RecordDlqMessageCount("whalewire.blockchainevent.queue.dlq", 3);
        act.Should().NotThrow();

        await using var stream = new MemoryStream();
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        output.Should().Contain("whalewire_dlq_messages_total");
        output.Should().Contain("queue=\"whalewire.blockchainevent.queue.dlq\"");
    }
}
