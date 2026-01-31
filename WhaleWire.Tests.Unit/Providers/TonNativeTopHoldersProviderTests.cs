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

public sealed class TonNativeTopHoldersProviderTests
{
    private const string BaseUrl = "https://toncenter.com";
    private const string ValidResponse = """
        [
            {"account": "0:ABC123", "balance": "1000000000"},
            {"account": "0:DEF456", "balance": "500000000"}
        ]
        """;

    [Fact]
    public async Task GetTopHoldersAsync_WithValidResponse_ReturnsHolders()
    {
        var httpClient = CreateHttpClient(ValidResponse, HttpStatusCode.OK);
        var logger = Substitute.For<ILogger<TonNativeTopHoldersProvider>>();
        var provider = new TonNativeTopHoldersProvider(httpClient, logger);

        var result = await provider.GetTopHoldersAsync(limit: 100);

        result.AssetIdentifier.Should().Be("TON");
        result.AssetType.Should().Be("native");
        result.Holders.Should().HaveCount(2);
        result.Holders[0].Address.Should().Be("0:ABC123");
        result.Holders[0].Balance.Should().Be(BigInteger.Parse("1000000000"));
    }

    [Fact]
    public async Task GetTopHoldersAsync_WithEmptyResponse_ReturnsEmpty()
    {
        var httpClient = CreateHttpClient("[]", HttpStatusCode.OK);
        var logger = Substitute.For<ILogger<TonNativeTopHoldersProvider>>();
        var provider = new TonNativeTopHoldersProvider(httpClient, logger);

        var result = await provider.GetTopHoldersAsync(limit: 100);

        result.Holders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopHoldersAsync_WithInvalidBalance_UsesZero()
    {
        var response = """[{"account": "0:ABC", "balance": "invalid"}]""";
        var httpClient = CreateHttpClient(response, HttpStatusCode.OK);
        var logger = Substitute.For<ILogger<TonNativeTopHoldersProvider>>();
        var provider = new TonNativeTopHoldersProvider(httpClient, logger);

        var result = await provider.GetTopHoldersAsync(limit: 100);

        result.Holders[0].Balance.Should().Be(BigInteger.Zero);
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
