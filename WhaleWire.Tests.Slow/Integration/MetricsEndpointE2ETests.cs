using FluentAssertions;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// E2E tests that hit /metrics and verify WhaleWire metrics appear.
/// </summary>
public sealed class MetricsEndpointE2ETests(WhaleWireWebApplicationFactory factory)
    : IClassFixture<WhaleWireWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task MetricsEndpoint_ReturnsDiscoveryMetric()
    {
        await Task.Delay(2500);

        var response = await _client.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("whalewire_discovery_addresses_total");
        content.Should().Contain("whalewire_discovery_addresses_total 4");
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsCircuitBreakerMetric()
    {
        var response = await _client.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("whalewire_circuit_breaker_state");
    }

    [Fact]
    public async Task MetricsEndpoint_ReturnsEventLagAndDlqMetrics()
    {
        await Task.Delay(2500);

        var response = await _client.GetAsync("/metrics");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("whalewire_event_lag_seconds");
        content.Should().Contain("whalewire_dlq_messages_total");
    }
}
