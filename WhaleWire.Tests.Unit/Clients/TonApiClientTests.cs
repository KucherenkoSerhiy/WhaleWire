using System.Net;
using System.Numerics;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WhaleWire.Domain;
using WhaleWire.Infrastructure.Ingestion.Clients;
using WhaleWire.Infrastructure.Ingestion.Configuration;

namespace WhaleWire.Tests.Unit.Clients;

public sealed class TonApiClientTests
{
    private const string BaseUrl = "https://tonapi.io";
    private const string TestAddress = "0:ABC123";
    
    private readonly IOptions<TonApiOptions> _options = Options.Create(new TonApiOptions
    {
        BaseUrl = BaseUrl,
        DefaultLimit = 100,
        DelayBetweenRequestsMs = 0
    });

    [Fact]
    public async Task GetEventsAsync_WithValidResponse_ReturnsEvents()
    {
        var response = """
            {
                "transactions": [
                    {"lt": 12345, "hash": "abc123", "utime": 1700000000},
                    {"lt": 12346, "hash": "def456", "utime": 1700000100}
                ]
            }
            """;
        var httpClient = CreateHttpClient(response, HttpStatusCode.OK);
        var client = new TonApiClient(httpClient, _options);

        var events = await client.GetEventsAsync(TestAddress, null, 100);

        events.Should().HaveCount(2);
        events[0].Cursor.Primary.Should().Be(12345);
        events[0].Cursor.Secondary.Should().Be("abc123");
        events[0].Chain.Should().Be("ton");
        events[0].Provider.Should().Be("tonapi");
    }

    [Fact]
    public async Task GetEventsAsync_WithNullBytes_SanitizesJson()
    {
        var responseWithNullBytes = """
            {
                "transactions": [
                    {"lt": 123, "hash": "test", "utime": 1700000000, "comment": "text\u0000null"}
                ]
            }
            """;
        var httpClient = CreateHttpClient(responseWithNullBytes, HttpStatusCode.OK);
        var client = new TonApiClient(httpClient, _options);

        var events = await client.GetEventsAsync(TestAddress, null, 100);

        events.Should().HaveCount(1);
        events[0].RawJson.Should().NotContain("\\u0000");
        events[0].RawJson.Should().NotContain("\u0000");
    }

    [Fact]
    public async Task GetEventsAsync_WithCursor_PassesAfterLt()
    {
        var response = """{"transactions": []}""";
        HttpRequestMessage? capturedRequest = null;
        
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(BaseUrl) };
        var client = new TonApiClient(httpClient, _options);
        var cursor = new Cursor(100, "hash100");

        await client.GetEventsAsync(TestAddress, cursor, 10);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.Query.Should().Contain("after_lt=100");
    }

    [Fact]
    public async Task GetEventsAsync_GeneratesEventId()
    {
        var response = """
            {
                "transactions": [
                    {"lt": 999, "hash": "testhash", "utime": 1700000000}
                ]
            }
            """;
        var httpClient = CreateHttpClient(response, HttpStatusCode.OK);
        var client = new TonApiClient(httpClient, _options);

        var events = await client.GetEventsAsync(TestAddress, null, 100);

        events[0].EventId.Should().NotBeNullOrEmpty();
        events[0].EventId.Should().HaveLength(16);
    }

    [Fact]
    public async Task GetEventsAsync_WithEmptyTransactions_ReturnsEmpty()
    {
        var response = """{"transactions": []}""";
        var httpClient = CreateHttpClient(response, HttpStatusCode.OK);
        var client = new TonApiClient(httpClient, _options);

        var events = await client.GetEventsAsync(TestAddress, null, 100);

        events.Should().BeEmpty();
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
