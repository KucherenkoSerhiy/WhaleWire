using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WhaleWire.Application.CorrelationId;
using WhaleWire.Application.Metrics;
using WhaleWire.Configuration;
using WhaleWire.Handlers;
using WhaleWire.Infrastructure.Messaging;
using WhaleWire.Infrastructure.Messaging.CorrelationId;
using WhaleWire.Infrastructure.Notifications;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Tests.Fakes;

namespace WhaleWire.Tests.Integration.TestFixtures;

/// <summary>
/// Same as WhaleWireIntegrationFixture but captures logs for CorrelationId verification.
/// Handler is invoked directly (no RabbitMQ consumer); test sets ICorrelationIdAccessor before calling handler.
/// </summary>
public sealed class WhaleWireIntegrationFixtureWithLogCapture : IAsyncLifetime
{
    private readonly LogCaptureProvider _logCapture = new();
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

    public IServiceProvider Services { get; private set; } = null!;
    public IReadOnlyList<string> CapturedLogs => _logCapture.Messages;

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
                ["CircuitBreaker:DurationOfBreakMinutes"] = "1"
            }).Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(_logCapture));
        services.AddPersistence(_postgresContainer.GetConnectionString());
        services.AddMessaging(configuration, _rabbitMqContainer.GetConnectionString());
        services.AddNotifications();
        services.AddSingleton<IWhaleWireMetrics, NullWhaleWireMetrics>();
        services.Configure<CircuitBreakerOptions>(configuration.GetSection("CircuitBreaker"));
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.AddScoped<BlockchainEventHandler>();

        Services = services.BuildServiceProvider();

        await ApplyMigrationsAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WhaleWireDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
}
