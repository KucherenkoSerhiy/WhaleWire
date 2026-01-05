using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WhaleWire.Application.Messaging;
using WhaleWire.Infrastructure.Messaging.Configuration;
using WhaleWire.Infrastructure.Messaging.Connections;

namespace WhaleWire.Infrastructure.Messaging.Consumers;

public sealed class RabbitMqConsumerService<T> : BackgroundService where T : class
{
    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    
    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerService<T>> _logger;
    private readonly RabbitMqRetryOptions _options;
    private readonly Timer _cleanupTimer;
    
    public RabbitMqConsumerService(RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqConsumerService<T>> logger,
        IOptions<RabbitMqRetryOptions> options)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _cleanupTimer = new Timer(_ => CleanupOldRetries(), null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.GetChannelAsync(stoppingToken);

        var exchangeName = GetExchangeName();
        var queueName = GetQueueName();
        var routingKey = GetRoutingKey();

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);
        
        var dlqName = $"{queueName}.dlq";
        await channel.QueueDeclareAsync(
            queue: dlqName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "" },
            { "x-dead-letter-routing-key", dlqName }
        };
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs!,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await ConsumeReceivedEvent(channel, eventArgs.DeliveryTag, eventArgs.Body, stoppingToken);
        };
        
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        // Keep alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static string GetExchangeName() => $"whalewire.{typeof(T).Name.ToLowerInvariant()}";
    private static string GetQueueName() => $"whalewire.{typeof(T).Name.ToLowerInvariant()}.queue";
    private static string GetRoutingKey() => $"{typeof(T).Name.ToLowerInvariant()}";

    private async Task ConsumeReceivedEvent(IChannel channel,
        ulong deliveryTag, ReadOnlyMemory<byte> messageBody, CancellationToken stoppingToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<T>(messageBody.Span, JsonOptions);
            if (message is null)
            {
                _logger.LogWarning("Failed to deserialize message of type {Type}", typeof(T).Name);
                await channel.BasicNackAsync(deliveryTag,
                    multiple: false, requeue: false, stoppingToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageConsumer<T>>();
            await handler.HandleAsync(message, stoppingToken);

            await channel.BasicAckAsync(deliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception e)
        {
            await HandleErrorsAsync(channel, deliveryTag, stoppingToken, e);
        }
    }

    
    // Track retry attempts for delivery tag
    private readonly Dictionary<ulong, int> _retryAttempts = new();

    private async Task HandleErrorsAsync(IChannel channel, ulong deliveryTag, CancellationToken stoppingToken, Exception exception)
    {
        var attempts = _retryAttempts.GetValueOrDefault(deliveryTag, 0);
        attempts++;
        _retryAttempts[deliveryTag] = attempts;
        
        _logger.LogError(
            exception,
            "Error processing message of type {Type} (attempt {Attempt}/{MaxRetries})",
            typeof(T).Name,
            attempts,
            _options.MaxRetries);
        
        if (attempts >= _options.MaxRetries)
        {
            // Max retries reached - reject without requeue (goes to dead-letter if configured)
            _logger.LogError(
                "Message failed {MaxRetries} times, rejecting without requeue. Type: {Type}",
                _options.MaxRetries,
                typeof(T).Name);

            await channel.BasicNackAsync(
                deliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: stoppingToken);

            _retryAttempts.Remove(deliveryTag);
        }
        else
        {
            // Apply exponential backoff before requeue
            var delay = attempts <= _options.RetryDelays.Length 
                ? _options.RetryDelays[attempts - 1] 
                : _options.RetryDelays[^1];

            _logger.LogWarning(
                "Requeuing message after {Delay}s delay (attempt {Attempt}/{MaxRetries})",
                delay.TotalSeconds,
                attempts,
                _options.MaxRetries);

            await Task.Delay(delay, stoppingToken);

            await channel.BasicNackAsync(
                deliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: stoppingToken);
        }
    }
    
    private void CleanupOldRetries()
    {
        // Remove entries older than 1 hour (assuming they're stale)
        // This is a simplified version - ideally track timestamps
        if (_retryAttempts.Count > 1000)
        {
            _retryAttempts.Clear();
            _logger.LogDebug("Cleared retry attempts cache");
        }
    }
}