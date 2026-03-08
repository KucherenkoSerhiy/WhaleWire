using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.CorrelationId;
using WhaleWire.Domain;
using WhaleWire.Handlers;
using WhaleWire.Messages;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// Integration test that verifies CorrelationId appears in handler logs when set via ICorrelationIdAccessor.
/// Invokes handler directly (no RabbitMQ consumer); proves handler logs the accessor's CorrelationId.
/// </summary>
public sealed class CorrelationIdHandlerIntegrationTests(WhaleWireIntegrationFixtureWithLogCapture fixture)
    : IClassFixture<WhaleWireIntegrationFixtureWithLogCapture>
{
    [Fact]
    public async Task HandlerInvokedWithCorrelationId_CorrelationIdAppearsInCapturedLogs()
    {
        var expectedCorrelationId = "integration-correlation-" + Guid.NewGuid().ToString("N");
        var eventId = $"handler-correlation-{Guid.NewGuid()}";

        using var scope = fixture.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICorrelationIdAccessor>();
        accessor.CorrelationId = expectedCorrelationId;

        var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();
        var evt = new BlockchainEvent
        {
            EventId = eventId,
            Chain = "ton",
            Provider = "tonapi",
            Address = "0:HANDLER_CORRELATION_TEST",
            Cursor = new Cursor(1, "hash1"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """{"in_msg": {"value": "1000000000"}}"""
        };

        await handler.HandleAsync(evt);

        fixture.CapturedLogs.Should().Contain(
            log => log.Contains(expectedCorrelationId),
            "CorrelationId from ICorrelationIdAccessor should appear in handler logs");
    }
}
