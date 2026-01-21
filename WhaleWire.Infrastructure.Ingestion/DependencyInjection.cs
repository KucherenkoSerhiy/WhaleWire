using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Infrastructure.Ingestion.Clients;
using WhaleWire.Infrastructure.Ingestion.Configuration;
using WhaleWire.Infrastructure.Ingestion.Workers;

namespace WhaleWire.Infrastructure.Ingestion;

public static class DependencyInjection
{
    public static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        services.AddOptions<TonApiOptions>()
            .BindConfiguration(TonApiOptions.SectionName);

        services.AddOptions<IngestionOptions>()
            .BindConfiguration(IngestionOptions.SectionName);

        services.AddOptions<WhaleWire.Application.UseCases.DiscoveryOptions>()
            .BindConfiguration(WhaleWire.Application.UseCases.DiscoveryOptions.SectionName);

        services.AddHttpClient<IBlockchainClient, TonApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TonApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            
            if (!string.IsNullOrEmpty(options.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        services.AddOptions<ChainstackOptions>()
            .BindConfiguration(ChainstackOptions.SectionName);

        services.AddHttpClient<WhaleWire.Application.UseCases.ITopAccountsClient, ChainstackTopAccountsClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ChainstackOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiUrl);
        });

        services.AddHostedService<DiscoveryWorkerService>();
        services.AddHostedService<IngestionWorkerService>();

        return services;
    }
}