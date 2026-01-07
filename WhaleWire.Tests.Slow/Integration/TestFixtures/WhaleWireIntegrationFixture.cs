using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WhaleWire.Infrastructure.Messaging;
using WhaleWire.Infrastructure.Persistence;

namespace WhaleWire.Tests.Integration.TestFixtures;

public class WhaleWireIntegrationFixture : IAsyncLifetime
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

    public IServiceProvider Services { get; private set; } = null!;

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
                ["RabbitMqRetry:RetryDelays:1"] = "00:00:02"
            }).Build();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPersistence(_postgresContainer.GetConnectionString());
        services.AddMessaging(configuration, _rabbitMqContainer.GetConnectionString());

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