namespace WhaleWire.Application.Metrics;

public interface IWhaleWireMetrics
{
    void RecordEventsIngested(int count, string chain);
    void RecordAlertFired(string assetId, string direction);
}
