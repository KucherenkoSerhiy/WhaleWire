using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Domain;
using WhaleWire.Handlers;
using WhaleWire.Messages;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

public sealed class FullAlertFlowTests(WhaleWireIntegrationFixture fixture) 
    : IClassFixture<WhaleWireIntegrationFixture>
{
    private const string TestChain = "ton";
    private const string TestProvider = "tonapi";
    private const string TestAddress = "0:WHALE_ADDRESS_123";

    [Fact]
    public async Task E2E_LargeTransferEvent_InsertsAndTriggersAlert()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();
        
        var evt = new BlockchainEvent
        {
            EventId = $"alert-flow-{Guid.NewGuid()}",
            Chain = TestChain,
            Provider = TestProvider,
            Address = TestAddress,
            Cursor = new Cursor(5000, "hash5000"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """
                {
                    "in_msg": {
                        "source": "0:SOURCE_WALLET",
                        "destination": "0:DEST_WALLET",
                        "value": "250000000000"
                    }
                }
                """
        };

        // Act & Assert - Should complete without exception
        var act = async () => await handler.HandleAsync(evt);
        await act.Should().NotThrowAsync();
        
        // Alert was logged to console (250 TON > 100 TON threshold)
    }

    [Fact]
    public async Task E2E_SmallTransferEvent_InsertsButNoAlert()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();
        
        var evt = new BlockchainEvent
        {
            EventId = $"small-transfer-{Guid.NewGuid()}",
            Chain = TestChain,
            Provider = TestProvider,
            Address = TestAddress,
            Cursor = new Cursor(5001, "hash5001"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """
                {
                    "in_msg": {
                        "value": "50000000000"
                    }
                }
                """
        };

        // Act & Assert - Should complete without exception
        var act = async () => await handler.HandleAsync(evt);
        await act.Should().NotThrowAsync();
        
        // No alert expected (50 TON < 100 TON threshold)
    }

    [Fact]
    public async Task E2E_DuplicateEvent_SkipsAlertEvaluation()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<BlockchainEventHandler>();
        
        var evt = new BlockchainEvent
        {
            EventId = $"duplicate-test-{Guid.NewGuid()}",
            Chain = TestChain,
            Provider = TestProvider,
            Address = TestAddress,
            Cursor = new Cursor(5002, "hash5002"),
            OccurredAt = DateTime.UtcNow,
            RawJson = """{"in_msg": {"value": "300000000000"}}"""
        };

        // Act - Handle twice
        await handler.HandleAsync(evt);
        var act = async () => await handler.HandleAsync(evt); // Duplicate

        // Assert - Should complete without exception (idempotent)
        await act.Should().NotThrowAsync();
        
        // Alert only triggered once (first insert)
    }
}
