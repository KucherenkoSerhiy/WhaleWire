namespace WhaleWire.Application.CorrelationId;

/// <summary>
/// Provides the correlation ID for the current message/request scope.
/// Set by the consumer before invoking the handler; used for structured logging.
/// </summary>
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}
