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
[Collection("DiscoveryMetrics")]
public sealed class DiscoveryMetricTests
{
    [Fact]
    public async Task DiscoveryWorkerService_WhenClientFails_DoesNotUpdateMetric()
    {
        await using (var beforeStream = new MemoryStream())
        {
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(beforeStream);
            var before = Encoding.UTF8.GetString(beforeStream.ToArray());
            var beforeMatch = System.Text.RegularExpressions.Regex.Match(before, @"whalewire_discovery_last_success_timestamp_seconds (\d+)");
            var timestampBefore = beforeMatch.Success ? long.Parse(beforeMatch.Groups[1].Value) : 0;

            var fixture = new DiscoveryHostFixture(new FailingTopAccountsClient());
            await fixture.InitializeAsync();
            try
            {
                await Task.Delay(2500);
            }
            finally
            {
                await fixture.DisposeAsync();
            }

            await using var afterStream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(afterStream);
            var after = Encoding.UTF8.GetString(afterStream.ToArray());
            var afterMatch = System.Text.RegularExpressions.Regex.Match(after, @"whalewire_discovery_last_success_timestamp_seconds (\d+)");
            var timestampAfter = afterMatch.Success ? long.Parse(afterMatch.Groups[1].Value) : 0;

            timestampAfter.Should().Be(timestampBefore, "when client fails, discovery should not update the last success timestamp");
        }
    }

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
            output.Should().Contain("whalewire_monitored_addresses");
            output.Should().Contain("whalewire_monitored_addresses 3");
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
            output.Should().Contain("whalewire_monitored_addresses");
            output.Should().Contain("whalewire_monitored_addresses 5");
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
