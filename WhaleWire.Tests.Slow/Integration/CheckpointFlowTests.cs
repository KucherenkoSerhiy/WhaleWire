using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WhaleWire.Application.Persistence;
using WhaleWire.Tests.Integration.TestFixtures;

namespace WhaleWire.Tests.Integration;

public class CheckpointFlowTests(WhaleWireIntegrationFixture fixture)
    : IClassFixture<WhaleWireIntegrationFixture>
{
    [Fact]
    public async Task EventInsertAndCheckpointUpdate_BothSuccess()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var checkpointRepo = scope.ServiceProvider.GetRequiredService<ICheckpointRepository>();

        var eventId = $"checkpoint-test-{Guid.NewGuid()}";

        // Act - Insert event
        var inserted = await eventRepo.UpsertEventIdempotentAsync(
            eventId, "ton", "EQCheckpoint", 100, "hash100", DateTime.UtcNow, "{}");
        
        // Assert - event successful
        inserted.Should().BeTrue();

        // Act - update checkpoint
        await checkpointRepo.UpdateCheckpointMonotonicAsync("ton", "EQCheckpoint", "test", 100, "hash100");
        
        // Assert - checkpoint updated
        var checkpoint = await checkpointRepo.GetCheckpointAsync("ton", "EQCheckpoint", "test");
        checkpoint.Should().NotBeNull();
        checkpoint!.LastLt.Should().Be(100);
    }
}