using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WhaleWire.Infrastructure.Messaging.Configuration;

namespace WhaleWire.Infrastructure.Messaging.Connections;

public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IChannel> GetChannelAsync(CancellationToken token = default)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _lock.WaitAsync(token);
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_options.ConnectionString)
            };

            _connection = await factory.CreateConnectionAsync(token);
            _channel = await _connection.CreateChannelAsync(cancellationToken: token);

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_connection is not null)
            await _connection.CloseAsync();
    }
}