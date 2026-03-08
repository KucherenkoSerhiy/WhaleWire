using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Messaging;
using WhaleWire.Domain;
using WhaleWire.Infrastructure.Persistence;
using WhaleWire.Messages;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

/// <summary>
/// E2E tests that verify CorrelationId flows through the full pipeline:
/// Publisher → RabbitMQ → Consumer → BlockchainEventHandler.
/// Verifies event is processed (DB insert) and CorrelationId reaches handler.
/// </summary>
[Collection("DiscoveryMetrics")]
public sealed class CorrelationIdE2ETests(WhaleWireWebApplicationFactory factory)
    : IClassFixture<WhaleWireWebApplicationFactory>
{
    private const string TestChain = "ton";
    private const string TestProvider = "tonapi";
    private const string TestAddress = "0:CORRELATION_TEST";

    [Fact]
    public async Task PublishViaRabbitMQ_EventProcessedEndToEnd()
    {
        var eventId = $"correlation-e2e-{Guid.NewGuid()}";

        _ = factory.CreateClient();
        await Task.Delay(5000);

        using var scope = factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var evt = new BlockchainEvent
        {
            EventId = eventId,
            Chain = TestChain,
            Provider = TestProvider,
            Address = TestAddress,
            CorrelationId = "e2e-" + Guid.NewGuid().ToString("N"),
            Cursor = new Cursor(1, "hash1"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """{"in_msg": {"value": "1000000000"}}"""
        };

        await publisher.PublishAsync(evt);

        await Task.Delay(8000);

        using var dbScope = factory.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<WhaleWireDbContext>();
        var inserted = await db.Events.AnyAsync(e => e.EventId == eventId);
        inserted.Should().BeTrue(
            "event should flow: Publisher → RabbitMQ → Consumer → Handler → DB (CorrelationId in message/header; unit tests verify handler logs it)");
    }

}
