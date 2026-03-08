using System.Text.Json;
using RabbitMQ.Client;
using WhaleWire.Application.Messaging;
using WhaleWire.Infrastructure.Messaging.Connections;

namespace WhaleWire.Infrastructure.Messaging.Publishers;

public sealed class RabbitMqPublisher(RabbitMqConnection connection): IMessagePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task PublishAsync<T>(T message, CancellationToken token = default) where T : class
    {
        var exchangeName = GetExchangeName<T>();
        var routingKey = GetRoutingKey<T>();

        var channel = await connection.GetChannelAsync(token);
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: token);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        var correlationId = GetCorrelationId(message) ?? Guid.NewGuid().ToString("N");
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId
        };

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: token);
    }

    private static string? GetCorrelationId<T>(T message) where T : class
    {
        var prop = typeof(T).GetProperty("CorrelationId");
        return prop?.GetValue(message) as string;
    }

    private static string GetExchangeName<T>() => $"whalewire.{typeof(T).Name.ToLowerInvariant()}";
    private static string GetRoutingKey<T>() => typeof(T).Name.ToLowerInvariant();
}