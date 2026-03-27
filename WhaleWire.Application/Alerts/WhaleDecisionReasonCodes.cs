namespace WhaleWire.Application.Alerts;

/// <summary>Stable <c>reasonCode</c> values for whale_decision audit JSON (ADR 0002).</summary>
public static class WhaleDecisionReasonCodes
{
    public const string NoQualifyingTransfer = "no_qualifying_transfer";
    public const string RawJsonParseError = "raw_json_parse_error";
}
