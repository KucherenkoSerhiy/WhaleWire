namespace WhaleWire.Application.Metrics;

public interface IWhaleWireMetrics
{
    void RecordEventsIngested(int count, string chain);
    void RecordAlertFired(string assetId, string direction);
    /// <summary>0=closed, 1=half-open, 2=open</summary>
    void RecordCircuitBreakerState(int state);
}
