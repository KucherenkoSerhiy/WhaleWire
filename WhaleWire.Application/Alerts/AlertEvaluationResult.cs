namespace WhaleWire.Application.Alerts;

public sealed record AlertEvaluationResult(IReadOnlyList<Alert> Alerts, bool JsonParseFailed)
{
    public static AlertEvaluationResult Success(IReadOnlyList<Alert> alerts) => new(alerts, false);

    public static AlertEvaluationResult ParseFailed() => new([], true);
}
