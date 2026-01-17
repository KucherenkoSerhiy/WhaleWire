using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Messaging;
using WhaleWire.Application.Persistence;
using WhaleWire.Domain;
using WhaleWire.Messages;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

public class IdempotencyIntegrationTests(WhaleWireIntegrationFixture fixture)
    : IClassFixture<WhaleWireIntegrationFixture>
{
    private readonly WhaleWireIntegrationFixture _fixture = fixture;

    [Fact]
    public async Task PublishSameEventTwice_OnlyOneRowInDatabase()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var testEvent = new BlockchainEvent
        {
            EventId = $"integration-test-{Guid.NewGuid()}",
            Chain = "ton-testnet",
            Provider = "ton-test-provider",
            Address = "EQIntegrationTest",
            Cursor = new Cursor(999, "integration-hash"),
            RawJson = """{"test": "integration"}""",
            OccurredAt = DateTime.UtcNow
        };

        // Act - Insert twice
        var firstInsert = await eventRepo.UpsertEventIdempotentAsync(
            testEvent.EventId, testEvent.Chain, testEvent.Address,
            testEvent.Cursor.Primary, testEvent.Cursor.Secondary, testEvent.OccurredAt, testEvent.RawJson);
        var secondInsert = await eventRepo.UpsertEventIdempotentAsync(
            testEvent.EventId, testEvent.Chain, testEvent.Address,
            testEvent.Cursor.Primary, testEvent.Cursor.Secondary, testEvent.OccurredAt, testEvent.RawJson);

        // Assert
        firstInsert.Should().BeTrue();
        secondInsert.Should().BeFalse();
    }
}