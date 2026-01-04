namespace WhaleWire.Application.Messaging;

public interface IMessageConsumer<T> where T: class
{
    Task HandleAsync<T>(T message, CancellationToken token = default) where T: class;
}