using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WhaleWire.Tests.Fakes;

namespace WhaleWire.Tests.Integration.TestFixtures;

/// <summary>
/// WebApplicationFactory that uses Testcontainers for Postgres and RabbitMQ.
/// Overrides ITopAccountsClient with FakeTopAccountsClient for fast discovery.
/// Captures logs for CorrelationId E2E verification.
/// </summary>
public sealed class WhaleWireWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly LogCaptureProvider _logCapture = new();

    public IReadOnlyList<string> CapturedLogs => _logCapture.Messages;
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("whalewire_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:RabbitMQ"] = _rabbitMq.GetConnectionString(),
                ["Discovery:Enabled"] = "true",
                ["Discovery:PollingIntervalSeconds"] = "1",
                ["Discovery:TopAccountsLimit"] = "10",
                ["Scheduler:Enabled"] = "false",
                ["Ingestion:Enabled"] = "false",
                ["MetricsCollector:IntervalSeconds"] = "1"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddScoped<WhaleWire.Application.UseCases.ITopAccountsClient>(
                _ => new FakeTopAccountsClient(addressCount: 4));
        });

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(_logCapture);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        return host;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}
