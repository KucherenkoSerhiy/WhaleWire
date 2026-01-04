namespace WhaleWire.Application.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken token = default) where T: class;
}