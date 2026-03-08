using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.Persistence;
using WhaleWire.Application.UseCases;
using WhaleWire.Configuration;
using WhaleWire.Infrastructure.Ingestion.Workers;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Metrics;
using WhaleWire.Tests.Fakes;

namespace WhaleWire.Tests.Integration.TestFixtures;

/// <summary>
/// Fixture that runs DiscoveryWorkerService for integration tests.
/// Uses 1s polling interval for fast execution.
/// </summary>
public sealed class DiscoveryHostFixture : IAsyncLifetime
{
    private readonly ITopAccountsClient _topAccountsClient;

    public DiscoveryHostFixture(ITopAccountsClient? topAccountsClient = null)
    {
        _topAccountsClient = topAccountsClient ?? new FakeTopAccountsClient(addressCount: 3);
    }

    private readonly PostgreSqlContainer _postgresContainer
        = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("whalewire_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

    public IHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discovery:Enabled"] = "true",
                ["Discovery:PollingIntervalSeconds"] = "1",
                ["Discovery:TopAccountsLimit"] = "10"
            }).Build();

        var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddPersistence(_postgresContainer.GetConnectionString());
                services.AddSingleton<IWhaleWireMetrics, WhaleWireMetrics>();
                services.AddSingleton<IBlockchainClient, FakeBlockchainClient>();
                services.AddScoped<ITopAccountsClient>(_ => _topAccountsClient);
                services.AddScoped<IDiscoveryUseCase, DiscoveryUseCase>();
                services.Configure<DiscoveryOptions>(configuration.GetSection("Discovery"));
                services.AddHostedService<DiscoveryWorkerService>();
            });

        Host = hostBuilder.Build();
        await ApplyMigrationsAsync();
        await Host.StartAsync();
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
    }
}
