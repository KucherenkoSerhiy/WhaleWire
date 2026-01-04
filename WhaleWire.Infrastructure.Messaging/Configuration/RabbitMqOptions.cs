namespace WhaleWire.Infrastructure.Messaging.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
}