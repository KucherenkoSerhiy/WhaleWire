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

    private static readonly Prometheus.Gauge CircuitBreakerState = Prometheus.Metrics.CreateGauge(
        "whalewire_circuit_breaker_state",
        "Circuit breaker state: 0=closed, 1=half-open, 2=open",
        new Prometheus.GaugeConfiguration { SuppressInitialValue = false });

    public void RecordEventsIngested(int count, string chain)
    {
        if (count > 0)
            EventsIngested.WithLabels(chain).Inc(count);
    }

    public void RecordAlertFired(string assetId, string direction)
    {
        AlertsFired.WithLabels(assetId, direction).Inc();
    }

    public void RecordCircuitBreakerState(int state)
    {
        CircuitBreakerState.Set(state);
    }
}
