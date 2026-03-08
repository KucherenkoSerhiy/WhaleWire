using WhaleWire.Application.CorrelationId;

namespace WhaleWire.Infrastructure.Messaging.CorrelationId;

/// <summary>
/// Scoped implementation. Set by RabbitMqConsumerService before invoking handler.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string? CorrelationId { get; set; }
}
