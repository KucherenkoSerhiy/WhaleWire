namespace WhaleWire.Application.Alerts;

public interface IWhaleDecisionAuditLogger
{
    void Log(WhaleDecisionRecord decision);
}
