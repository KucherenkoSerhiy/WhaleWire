using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WhaleWire.Application.Messaging;
using WhaleWire.Infrastructure.Messaging.Connections;

namespace WhaleWire.Infrastructure.Messaging.Consumers;

public sealed class RabbitMqConsumerService<T>(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumerService<T>> logger)
    : BackgroundService where T : class
{
    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await connection.GetChannelAsync(stoppingToken);

        var exchangeName = GetExchangeName();
        var queueName = GetQueueName();
        var routingKey = GetRoutingKey();

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
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
                logger.LogWarning("Failed to deserialize message of type {Type}", typeof(T).Name);
                await channel.BasicNackAsync(deliveryTag,
                    multiple: false, requeue: false, stoppingToken);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageConsumer<T>>();
            await handler.HandleAsync(message, stoppingToken);

            await channel.BasicAckAsync(deliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing message of type {Type}", typeof(T).Name);
            await channel.BasicNackAsync(deliveryTag,
                multiple: false, requeue: true, cancellationToken: stoppingToken);
        }
    }
}