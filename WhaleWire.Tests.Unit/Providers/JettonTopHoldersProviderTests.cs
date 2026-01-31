using System.Net;
using System.Numerics;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NSubstitute;
using WhaleWire.Infrastructure.Ingestion.Providers;

namespace WhaleWire.Tests.Unit.Providers;

public sealed class JettonTopHoldersProviderTests
{
    private const string BaseUrl = "https://toncenter.com";
    private const string Symbol = "NOT";
    private const string MasterAddress = "EQ...NOT";
    private const string ValidResponse = """
        {
            "jetton_wallets": [
                {"owner": "0:ABC123", "balance": "999999999999999"},
                {"owner": "0:DEF456", "balance": "500000000000000"}
            ]
        }
        """;

    [Fact]
    public async Task GetTopHoldersAsync_WithValidResponse_ReturnsHolders()
    {
        var httpClient = CreateHttpClient(ValidResponse, HttpStatusCode.OK);
        var logger = Substitute.For<ILogger<JettonTopHoldersProvider>>();
        var provider = new JettonTopHoldersProvider(httpClient, MasterAddress, Symbol, logger);

        var result = await provider.GetTopHoldersAsync(limit: 100);

        result.AssetIdentifier.Should().Be(Symbol);
        result.AssetType.Should().Be("jetton");
        result.Holders.Should().HaveCount(2);
        result.Holders[0].Address.Should().Be("0:ABC123");
        result.Holders[0].Balance.Should().Be(BigInteger.Parse("999999999999999"));
    }

    [Fact]
    public async Task GetTopHoldersAsync_WithEmptyWallets_ReturnsEmpty()
    {
        var response = """{"jetton_wallets": []}""";
        var httpClient = CreateHttpClient(response, HttpStatusCode.OK);
        var logger = Substitute.For<ILogger<JettonTopHoldersProvider>>();
        var provider = new JettonTopHoldersProvider(httpClient, MasterAddress, Symbol, logger);

        var result = await provider.GetTopHoldersAsync(limit: 100);

        result.Holders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopHoldersAsync_WithHugeBalance_ParsesCorrectly()
    {
        var hugeBalance = "999999999999999999999999999999999";
        var response = $$"""{"jetton_wallets": [{"owner": "0:ABC", "balance": "{{hugeBalance}}"}]}""";
        var httpClient = CreateHttpClient(response, HttpStatusCode.OK);
        var logger = Substitute.For<ILogger<JettonTopHoldersProvider>>();
        var provider = new JettonTopHoldersProvider(httpClient, MasterAddress, Symbol, logger);

        var result = await provider.GetTopHoldersAsync(limit: 100);

        result.Holders[0].Balance.Should().Be(BigInteger.Parse(hugeBalance));
    }

    private static HttpClient CreateHttpClient(string responseContent, HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }
}
