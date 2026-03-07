namespace WhaleWire.Application.Metrics;

/// <summary>
/// No-op implementation for testing or when metrics are disabled.
/// </summary>
public sealed class NullWhaleWireMetrics : IWhaleWireMetrics
{
    public void RecordEventsIngested(int count, string chain) { }
    public void RecordAlertFired(string assetId, string direction) { }
    public void RecordCircuitBreakerState(int state) { }
    public void RecordEventLag(string chain, string address, double lagSeconds) { }
    public void RecordDlqMessageCount(string queue, int count) { }
    public void RecordDiscoveryAddresses(int count) { }
}
