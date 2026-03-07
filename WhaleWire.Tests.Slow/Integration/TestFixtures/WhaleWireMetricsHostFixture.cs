using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WhaleWire.Application.Metrics;
using WhaleWire.Configuration;
using WhaleWire.Handlers;
using WhaleWire.Infrastructure.Messaging;
using WhaleWire.Infrastructure.Notifications;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Metrics;
using WhaleWire.Messages;
using WhaleWire.Services;

namespace WhaleWire.Tests.Integration.TestFixtures;

/// <summary>
/// Fixture that builds an IHost with metrics collectors for integration tests.
/// Uses 1s collection interval for fast test execution.
/// </summary>
public sealed class WhaleWireMetricsHostFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer
        = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("whalewire_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

    private readonly RabbitMqContainer _rabbitMqContainer
        = new RabbitMqBuilder("rabbitmq:4-management")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

    public IHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMqRetry:MaxRetries"] = "3",
                ["RabbitMqRetry:RetryDelays:0"] = "00:00:01",
                ["RabbitMqRetry:RetryDelays:1"] = "00:00:02",
                ["CircuitBreaker:ExceptionsAllowedBeforeBreaking"] = "5",
                ["CircuitBreaker:DurationOfBreakMinutes"] = "1",
                ["MetricsCollector:IntervalSeconds"] = "1"
            }).Build();

        var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddPersistence(_postgresContainer.GetConnectionString());
                services.AddMessaging(configuration, _rabbitMqContainer.GetConnectionString());
                services.AddNotifications();
                services.AddSingleton<IWhaleWireMetrics, WhaleWireMetrics>();

                services.Configure<CircuitBreakerOptions>(configuration.GetSection("CircuitBreaker"));
                services.Configure<MetricsCollectorOptions>(configuration.GetSection("MetricsCollector"));
                services.AddScoped<BlockchainEventHandler>();
                services.AddMessageConsumer<BlockchainEvent, BlockchainEventHandler>();

                services.AddSingleton<IConnection>(_ =>
                {
                    var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqContainer.GetConnectionString()) };
                    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                });

                services.AddHostedService<EventLagMetricsCollector>();
                services.AddHostedService<DlqMetricsCollector>();
            });

        Host = hostBuilder.Build();
        await Host.StartAsync();

        await ApplyMigrationsAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WhaleWireDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Host.StopAsync();
        Host.Dispose();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}
