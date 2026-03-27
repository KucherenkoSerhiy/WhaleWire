using Microsoft.Extensions.Logging;
using WhaleWire.Application.Alerts;

namespace WhaleWire.Infrastructure.Notifications;

public sealed class WhaleDecisionAuditLogger(ILogger<WhaleDecisionAuditLogger> logger) : IWhaleDecisionAuditLogger
{
    public void Log(WhaleDecisionRecord decision) =>
        logger.LogInformation("{WhaleDecision}", decision.ToJsonLine());
}
