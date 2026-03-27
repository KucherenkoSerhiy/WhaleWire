namespace WhaleWire.Application.Alerts;

public sealed class NullWhaleDecisionAuditLogger : IWhaleDecisionAuditLogger
{
    public static readonly NullWhaleDecisionAuditLogger Instance = new();

    public void Log(WhaleDecisionRecord decision) { }
}
