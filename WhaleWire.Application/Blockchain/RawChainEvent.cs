namespace WhaleWire.Application.Blockchain;

public sealed record RawChainEvent(
    string Chain,
    string Provider,
    string Address,
    Cursor Cursor,
    string Hash,
    DateTime OccurredAt,
    string RawJson);