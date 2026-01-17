using FluentAssertions;
using WhaleWire.Infrastructure.Persistence.Exceptions;
using WhaleWire.Tests.Common.Builders;

namespace WhaleWire.Tests.Unit.Entities;

public sealed class CheckpointTests
{
    private const long OldLt = 100;
    private const string OldHash = "hash-100";
    private const long NewLt = 200;
    private const string NewHash = "hash-200";
    
    [Fact]
    public void Update_WithSameLtAndDifferentHash_ThrowsCheckpointConflictException()
    {
        var checkpoint = ObjectMother.Checkpoints.CreateEntity(OldLt, OldHash);
        
        var act = () => checkpoint.Update(OldLt, NewHash);

        act.Should().Throw<CheckpointConflictException>();
    }
    
    [Fact]
    public void Update_WithHigherLt_UpdatesCheckpoint()
    {
        var checkpoint = ObjectMother.Checkpoints.CreateEntity(OldLt, OldHash);
        var originalUpdatedAt = checkpoint.UpdatedAt;
        
        Thread.Sleep(TimeSpan.FromMilliseconds(10));
        checkpoint.Update(NewLt, NewHash);

        checkpoint.LastLt.Should().Be(NewLt);
        checkpoint.LastHash.Should().Be(NewHash);
        checkpoint.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_WithLowerLt_DoesNotUpdate()
    {
        var checkpoint = ObjectMother.Checkpoints.CreateEntity(NewLt, NewHash);
        var originalUpdatedAt = checkpoint.UpdatedAt;

        checkpoint.Update(OldLt, OldHash);

        checkpoint.LastLt.Should().Be(NewLt);
        checkpoint.LastHash.Should().Be(NewHash);
        checkpoint.UpdatedAt.Should().Be(originalUpdatedAt);
    }
}
