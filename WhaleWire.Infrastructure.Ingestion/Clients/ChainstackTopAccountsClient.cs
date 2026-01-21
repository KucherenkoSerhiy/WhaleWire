using System.Text.Json;
using System.Text.Json.Serialization;
using WhaleWire.Application.UseCases;

namespace WhaleWire.Infrastructure.Ingestion.Clients;

public sealed class ChainstackTopAccountsClient(HttpClient httpClient) : ITopAccountsClient
{
    public async Task<IReadOnlyList<TopAccount>> GetTopAccountsByBalanceAsync(
        int limit,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"/api/v3/topAccountsByBalance?limit={limit}",
            ct);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<ChainstackResponse>(json);

        return result?.Accounts?
            .Select(a => new TopAccount(a.Address, a.Balance))
            .ToList() ?? [];
    }

    private sealed record ChainstackResponse(
        [property: JsonPropertyName("accounts")] Account[]? Accounts);

    private sealed record Account(
        [property: JsonPropertyName("address")] string Address,
        [property: JsonPropertyName("balance")] long Balance);
}
