using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Application.UseCases;
using WhaleWire.Infrastructure.Ingestion.Clients;
using WhaleWire.Infrastructure.Ingestion.Configuration;
using WhaleWire.Infrastructure.Ingestion.Providers;
using WhaleWire.Infrastructure.Ingestion.Workers;

namespace WhaleWire.Infrastructure.Ingestion;

public static class DependencyInjection
{
    public static IServiceCollection AddIngestion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTonEcosystem(configuration);

        // Composite client that unions all asset providers
        services.AddScoped<ITopAccountsClient, CompositeTopAccountsClient>();

        // Workers
        services.AddHostedService<DiscoveryWorkerService>();
        services.AddHostedService<IngestionWorkerService>();

        return services;
    }

    private static IServiceCollection AddTonEcosystem(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TonAPI for blockchain events ingestion
        services.AddOptions<TonApiOptions>()
            .BindConfiguration(TonApiOptions.SectionName);

        services.AddHttpClient<IBlockchainClient, TonApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TonApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            
            if (!string.IsNullOrEmpty(options.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        // TonCenter for top accounts discovery
        services.AddOptions<TonCenterOptions>()
            .BindConfiguration(TonCenterOptions.SectionName);

        services.AddOptions<IngestionOptions>()
            .BindConfiguration(IngestionOptions.SectionName);

        // Register TON native provider
        services.AddHttpClient<TonNativeTopHoldersProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<TonCenterOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            
            if (!string.IsNullOrEmpty(options.ApiKey))
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
        });
        services.AddTransient<IAssetTopHoldersProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<TonNativeTopHoldersProvider>>();
            var client = httpClientFactory.CreateClient(nameof(TonNativeTopHoldersProvider));
            return new TonNativeTopHoldersProvider(client, logger);
        });

        // Register jetton providers from config
        var tonCenterOptions = configuration
            .GetSection(TonCenterOptions.SectionName)
            .Get<TonCenterOptions>();

        if (tonCenterOptions?.Jettons != null)
        {
            foreach (var jettonConfig in tonCenterOptions.Jettons)
            {
                var jetton = jettonConfig; // Capture for closure
                
                services.AddTransient<IAssetTopHoldersProvider>(sp =>
                {
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var options = sp.GetRequiredService<IOptions<TonCenterOptions>>().Value;
                    var logger = sp.GetRequiredService<ILogger<JettonTopHoldersProvider>>();
                    
                    var client = httpClientFactory.CreateClient($"TonCenter-{jetton.Symbol}");
                    client.BaseAddress = new Uri(options.BaseUrl);
                    
                    if (!string.IsNullOrEmpty(options.ApiKey))
                        client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
                    
                    return new JettonTopHoldersProvider(client, jetton.MasterAddress, jetton.Symbol, logger);
                });
            }
        }

        return services;
    }
}
