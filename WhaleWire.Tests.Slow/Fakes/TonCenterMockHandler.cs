using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WhaleWire.Tests.Fakes;

/// <summary>
/// Mock HttpMessageHandler that returns TonCenter topAccountsByBalance format.
/// </summary>
public sealed class TonCenterMockHandler : DelegatingHandler
{
    private readonly int _accountCount;

    public TonCenterMockHandler(int accountCount = 5)
    {
        _accountCount = accountCount;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.RequestUri?.PathAndQuery.StartsWith("/api/v3/topAccountsByBalance") == true)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var accounts = Enumerable.Range(0, _accountCount)
            .Select(i => new { account = $"0:MOCK_ADDR_{i}", balance = "1000000000" })
            .ToArray();

        var json = JsonSerializer.Serialize(accounts);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
