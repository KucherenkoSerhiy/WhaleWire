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

    private static readonly Prometheus.Gauge EventLagSeconds = Prometheus.Metrics.CreateGauge(
        "whalewire_event_lag_seconds",
        "Seconds since last event per address",
        new Prometheus.GaugeConfiguration { LabelNames = ["chain", "address"] });

    private static readonly Prometheus.Gauge DlqMessages = Prometheus.Metrics.CreateGauge(
        "whalewire_dlq_messages_total",
        "Number of messages in dead letter queue (one message = alert)",
        new Prometheus.GaugeConfiguration { LabelNames = ["queue"] });

    private static readonly Prometheus.Gauge DiscoveryAddresses = Prometheus.Metrics.CreateGauge(
        "whalewire_discovery_addresses_total",
        "Addresses discovered in last successful discovery cycle",
        new Prometheus.GaugeConfiguration { SuppressInitialValue = false });

    private static readonly Prometheus.Gauge DiscoveryLastSuccessTimestamp = Prometheus.Metrics.CreateGauge(
        "whalewire_discovery_last_success_timestamp_seconds",
        "Unix timestamp of last successful discovery cycle (for WhaleWireDiscoveryFailed alert)",
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

    public void RecordEventLag(string chain, string address, double lagSeconds)
    {
        EventLagSeconds.WithLabels(chain, address).Set(lagSeconds);
    }

    public void RecordDlqMessageCount(string queue, int count)
    {
        DlqMessages.WithLabels(queue).Set(count);
    }

    public void RecordDiscoveryAddresses(int count)
    {
        DiscoveryAddresses.Set(count);
    }

    public void RecordDiscoveryLastSuccessTimestamp(long unixTimestampSeconds)
    {
        DiscoveryLastSuccessTimestamp.Set(unixTimestampSeconds);
    }
}
