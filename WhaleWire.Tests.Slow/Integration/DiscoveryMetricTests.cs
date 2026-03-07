using System.Text;
using FluentAssertions;
using Prometheus;
using WhaleWire.Tests.Fakes;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// Integration tests for discovery metric.
/// Uses FakeTopAccountsClient to verify the metric is recorded.
/// </summary>
public sealed class DiscoveryMetricTests
{
    [Fact]
    public async Task DiscoveryWorkerService_RunsAndRecordsMetric()
    {
        var fixture = new DiscoveryHostFixture();
        await fixture.InitializeAsync();
        try
        {
            await Task.Delay(2500);

            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().Contain("whalewire_discovery_addresses_total");
            output.Should().Contain("whalewire_discovery_addresses_total 3");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task DiscoveryWorkerService_WhenClientFails_DoesNotUpdateMetric()
    {
        var fixture = new DiscoveryHostFixture(new FailingTopAccountsClient());
        await fixture.InitializeAsync();
        try
        {
            await Task.Delay(2500);

            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().NotContain("whalewire_discovery_addresses_total 3");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task DiscoveryWithMockTonCenter_RecordsMetricFromRealProvider()
    {
        var fixture = new DiscoveryWithMockTonCenterFixture(expectedAddressCount: 5);
        await fixture.InitializeAsync();
        try
        {
            await Task.Delay(2500);

            await using var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            var output = Encoding.UTF8.GetString(stream.ToArray());
            output.Should().Contain("whalewire_discovery_addresses_total");
            output.Should().Contain("whalewire_discovery_addresses_total 5");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
