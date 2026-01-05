namespace WhaleWire.Infrastructure.Messaging.Configuration;

public sealed class RabbitMqRetryOptions
{
    public int MaxRetries { get; init; } = 3;

    public TimeSpan[] RetryDelays { get; init; } =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30)
    ];
}