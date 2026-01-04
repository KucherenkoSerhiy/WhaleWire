using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Messaging;
using WhaleWire.Infrastructure.Messaging.Configuration;
using WhaleWire.Infrastructure.Messaging.Connections;
using WhaleWire.Infrastructure.Messaging.Consumers;
using WhaleWire.Infrastructure.Messaging.Publishers;

namespace WhaleWire.Infrastructure.Messaging;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMessaging(string connectionString)
        {
            services.Configure<RabbitMqOptions>(options =>
                options.ConnectionString = connectionString);

            services.AddSingleton<RabbitMqConnection>();
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            return services;
        }

        public IServiceCollection AddMessageConsumer<TMessage, THandler>()
            where TMessage : class
            where THandler : class, IMessageConsumer<TMessage>
        {
            services.AddScoped<IMessageConsumer<TMessage>, THandler>();
            services.AddHostedService<RabbitMqConsumerService<TMessage>>();
            return services;
        }
    }
}