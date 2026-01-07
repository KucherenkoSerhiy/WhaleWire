using AutoFixture;
using FluentAssertions;
using WhaleWire.Infrastructure.Persistence.Repositories;
using WhaleWire.Tests.Common.Builders;
using WhaleWire.Tests.Common.Fixtures;

namespace WhaleWire.Tests.Unit.Repositories;

public sealed class CheckpointRepositoryTests : InMemoryDbContextFixture
{
    private readonly CheckpointRepository _repository;
    private readonly Fixture _fixture = new();

    public CheckpointRepositoryTests()
    {
        _repository = new CheckpointRepository(Context);
    }

    [Fact]
    public async Task UpdateCheckpoint_WithExplicitlyRelatedValues_Succeeds()
    {
        // Arrange
        var targetLt = _fixture.Create<long>();
        var checkpoint = ObjectMother.Checkpoints
            .WithConsistentLtAndHash(targetLt)
            .Build();
        var expectedLastHash = checkpoint.LastHash;

        // Act
        await _repository.UpdateCheckpointAsync(
            checkpoint.Chain, checkpoint.Address, checkpoint.Provider,
            checkpoint.LastLt, checkpoint.LastHash);

        // Assert
        var saved = await _repository.GetCheckpointAsync(
            checkpoint.Chain, checkpoint.Address, checkpoint.Provider);
        
        saved.Should().NotBeNull();
        saved!.LastLt.Should().Be(targetLt);
        saved.LastHash.Should().Be(expectedLastHash);
    }

    [Fact]
    public async Task UpdateCheckpoint_Progressive_MaintainsOnlyOneRow()
    {
        // Arrange
        var address = $"address-{_fixture.Create<string>()}";
        var hash1 = "hash-100";
        var lt1 = 100;
        var checkpoint1 = ObjectMother.Checkpoints
            .ForAddress(address)
            .WithLastLtAndHash(lt1, hash1)
            .Build();

        var hash2 = "hash-200";
        var lt2 = 200;
        var checkpoint2 = ObjectMother.Checkpoints
            .ForAddress(address)
            .WithLastLtAndHash(lt2, hash2)
            .Build();

        // Act
        await UpdateCheckpointAsync(checkpoint1);
        await UpdateCheckpointAsync(checkpoint2);

        // Assert
        var final = await _repository.GetCheckpointAsync(
            checkpoint2.Chain, checkpoint2.Address, checkpoint2.Provider);

        final!.LastLt.Should().Be(lt2);
        final.LastHash.Should().Be(hash2);
        Context.Checkpoints.Should().ContainSingle();
    }

    private Task UpdateCheckpointAsync(CheckpointTestData data) =>
        _repository.UpdateCheckpointAsync(
            data.Chain, data.Address, data.Provider, data.LastLt, data.LastHash);
}