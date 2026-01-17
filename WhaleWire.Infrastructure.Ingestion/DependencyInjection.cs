using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhaleWire.Application.Blockchain;
using WhaleWire.Infrastructure.Ingestion.Clients;
using WhaleWire.Infrastructure.Ingestion.Configuration;

namespace WhaleWire.Infrastructure.Ingestion;

public static class DependencyInjection
{
    public static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        services.AddOptions<TonApiOptions>()
            .BindConfiguration(TonApiOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddHttpClient<IBlockchainClient, TonApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TonApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            
            if (!string.IsNullOrEmpty(options.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        return services;
    }
}