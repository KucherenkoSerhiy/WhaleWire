namespace WhaleWire.Application.Metrics;

/// <summary>
/// No-op implementation for testing or when metrics are disabled.
/// </summary>
public sealed class NullWhaleWireMetrics : IWhaleWireMetrics
{
    public void RecordEventsIngested(int count, string chain) { }
    public void RecordAlertFired(string assetId, string direction) { }
}
