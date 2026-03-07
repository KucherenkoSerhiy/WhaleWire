using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.Metrics;
using WhaleWire.Application.UseCases;
using WhaleWire.Configuration;
using WhaleWire.Infrastructure.Ingestion.Configuration;
using WhaleWire.Infrastructure.Ingestion.Clients;
using WhaleWire.Infrastructure.Ingestion.Providers;
using WhaleWire.Infrastructure.Ingestion.Workers;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Metrics;
using WhaleWire.Tests.Fakes;

namespace WhaleWire.Tests.Integration.TestFixtures;

/// <summary>
/// Fixture that runs discovery with HTTP-mocked TonCenter (real TonNativeTopHoldersProvider).
/// Verifies the full discovery pipeline with real parsing.
/// </summary>
public sealed class DiscoveryWithMockTonCenterFixture : IAsyncLifetime
{
    private readonly int _expectedAddressCount;
    private readonly PostgreSqlContainer _postgresContainer
        = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("whalewire_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

    public DiscoveryWithMockTonCenterFixture(int expectedAddressCount = 5)
    {
        _expectedAddressCount = expectedAddressCount;
    }

    public IHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Discovery:Enabled"] = "true",
                ["Discovery:PollingIntervalSeconds"] = "1",
                ["Discovery:TopAccountsLimit"] = "10",
                ["TonCenter:BaseUrl"] = "https://dummy.toncenter.com",
                ["TonCenter:ApiKey"] = "",
                ["TonCenter:DelayBetweenRequestsMs"] = "0"
            }).Build();

        var mockHandler = new TonCenterMockHandler(_expectedAddressCount);

        var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddPersistence(_postgresContainer.GetConnectionString());
                services.AddSingleton<IWhaleWireMetrics, WhaleWireMetrics>();
                services.AddSingleton<IBlockchainClient, FakeBlockchainClient>();
                services.Configure<DiscoveryOptions>(configuration.GetSection("Discovery"));
                services.Configure<TonCenterOptions>(configuration.GetSection("TonCenter"));

                services.AddHttpClient<TonNativeTopHoldersProvider>()
                    .ConfigurePrimaryHttpMessageHandler(() => mockHandler)
                    .ConfigureHttpClient(client =>
                    {
                        client.BaseAddress = new Uri("https://dummy.toncenter.com");
                    });

                services.AddTransient<IAssetTopHoldersProvider>(sp =>
                {
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var logger = sp.GetRequiredService<ILogger<TonNativeTopHoldersProvider>>();
                    var client = httpClientFactory.CreateClient(nameof(TonNativeTopHoldersProvider));
                    return new TonNativeTopHoldersProvider(client, logger);
                });

                services.AddScoped<ITopAccountsClient, CompositeTopAccountsClient>();
                services.AddScoped<IDiscoveryUseCase, DiscoveryUseCase>();
                services.AddHostedService<DiscoveryWorkerService>();
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
    }
}
