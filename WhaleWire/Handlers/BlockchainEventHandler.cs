using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using WhaleWire.Application.Alerts;
using WhaleWire.Application.CorrelationId;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.Persistence;
using WhaleWire.Configuration;
using WhaleWire.Messages;

namespace WhaleWire.Handlers;

public sealed class BlockchainEventHandler : IMessageConsumer<BlockchainEvent>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICheckpointRepository _checkpointRepository;
    private readonly IAlertEvaluator _alertEvaluator;
    private readonly IAlertNotifier _alertNotifier;
    private readonly IWhaleDecisionAuditLogger _whaleDecisionAudit;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<BlockchainEventHandler> _logger;
    private readonly ResiliencePipeline _circuitBreaker;

    public BlockchainEventHandler(
        IEventRepository eventRepository,
        ICheckpointRepository checkpointRepository,
        IAlertEvaluator alertEvaluator,
        IAlertNotifier alertNotifier,
        IWhaleDecisionAuditLogger whaleDecisionAudit,
        ICorrelationIdAccessor correlationIdAccessor,
        IWhaleWireMetrics metrics,
        ILogger<BlockchainEventHandler> logger,
        IOptions<CircuitBreakerOptions> options)
    {
        _eventRepository = eventRepository;
        _checkpointRepository = checkpointRepository;
        _alertEvaluator = alertEvaluator;
        _alertNotifier = alertNotifier;
        _whaleDecisionAudit = whaleDecisionAudit;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
        _circuitBreaker = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = options.Value.ExceptionsAllowedBeforeBreaking,
                SamplingDuration = TimeSpan.FromMinutes(5),
                BreakDuration = TimeSpan.FromMinutes(options.Value.DurationOfBreakMinutes),
                ShouldHandle = new Polly.PredicateBuilder().Handle<Exception>(),
                OnOpened = _ => { metrics.RecordCircuitBreakerState(2); return default; },
                OnHalfOpened = _ => { metrics.RecordCircuitBreakerState(1); return default; },
                OnClosed = _ => { metrics.RecordCircuitBreakerState(0); return default; }
            })
            .Build();
        metrics.RecordCircuitBreakerState(0);
    }

    public async Task HandleAsync(BlockchainEvent message, CancellationToken token = default)
    {
        await _circuitBreaker.ExecuteAsync(async ct =>
        {
            _logger.LogInformation("Received CanonicalEventReady: {EventId} for {Chain}/{Address}. CorrelationId: {CorrelationId}",
                message.EventId, message.Chain, message.Address, _correlationIdAccessor.CorrelationId);

            var wasInserted = await _eventRepository.UpsertEventIdempotentAsync(
                eventId: message.EventId,
                chain: message.Chain,
                address: message.Address,
                lt: message.Cursor.Primary,
                txHash: message.Cursor.Secondary,
                blockTime: message.OccurredAt,
                rawJson: message.RawJson,
                ct);

            if (wasInserted)
            {
                await _checkpointRepository.UpdateCheckpointMonotonicAsync(
                    message.Chain,
                    message.Address,
                    message.Provider,
                    message.Cursor.Primary,
                    message.Cursor.Secondary,
                    ct);

                // Evaluate and send alerts for new events
                var evaluation = await _alertEvaluator.EvaluateAsync(message, ct);
                if (!evaluation.JsonParseFailed && evaluation.Alerts.Count == 0)
                {
                    _whaleDecisionAudit.Log(WhaleDecisionRecord.ForNoQualifyingTransfer(
                        message,
                        _correlationIdAccessor.CorrelationId));
                }

                foreach (var alert in evaluation.Alerts)
                {
                    await _alertNotifier.NotifyAsync(alert, ct);
                }
            }

            _logger.LogInformation(
                wasInserted
                    ? "Event {EventId} inserted successfully. CorrelationId: {CorrelationId}"
                    : "Event {EventId} already exists, skipped (idempotent). CorrelationId: {CorrelationId}",
                message.EventId, _correlationIdAccessor.CorrelationId);
        }, token);
    }
}