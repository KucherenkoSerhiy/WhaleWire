namespace WhaleWire.Application.Messaging;

public interface IMessageConsumer<T> where T: class
{
    Task HandleAsync(T message, CancellationToken token = default);
}