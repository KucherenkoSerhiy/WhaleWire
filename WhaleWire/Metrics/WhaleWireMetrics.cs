using WhaleWire.Application.Metrics;

namespace WhaleWire.Metrics;

public sealed class WhaleWireMetrics : IWhaleWireMetrics
{
    private static readonly Prometheus.Counter EventsIngested = Prometheus.Metrics.CreateCounter(
        "whalewire_events_ingested_total",
        "Total blockchain events ingested and published",
        new Prometheus.CounterConfiguration { LabelNames = ["chain"] });

    private static readonly Prometheus.Counter AlertsFired = Prometheus.Metrics.CreateCounter(
        "whalewire_alerts_fired_total",
        "Total whale alerts fired",
        new Prometheus.CounterConfiguration { LabelNames = ["asset", "direction"] });

    public void RecordEventsIngested(int count, string chain)
    {
        if (count > 0)
            EventsIngested.WithLabels(chain).Inc(count);
    }

    public void RecordAlertFired(string assetId, string direction)
    {
        AlertsFired.WithLabels(assetId, direction).Inc();
    }
}
