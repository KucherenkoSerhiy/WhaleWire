using FluentAssertions;
using WhaleWire.Infrastructure.Persistence.Repositories;
using WhaleWire.Tests.Common.Builders;
using WhaleWire.Tests.Common.Fixtures;

namespace WhaleWire.Tests.Unit.Repositories;

public sealed class CheckpointRepositoryTests : InMemoryDbContextFixture
{
    private const string Chain = "ton";
    private const string Address = "addr1";
    private const string Provider = "tonapi";
    private const long OldLt = 100;
    private const string OldHash = "hash-100";
    private const long NewLt = 200;
    private const string NewHash = "hash-200";

    private readonly CheckpointRepository _repository;

    public CheckpointRepositoryTests()
    {
        _repository = new CheckpointRepository(Context);
    }

    [Fact]
    public async Task GetCheckpoint_WhenExists_ReturnsCheckpoint()
    {
        var data = ObjectMother.Checkpoints.WithConsistentLtAndHash(OldLt).Build();
        await _repository.UpdateCheckpointMonotonicAsync(
            data.Chain, data.Address, data.Provider, data.LastLt, data.LastHash);

        var result = await _repository.GetCheckpointAsync(
            data.Chain, data.Address, data.Provider);

        result.Should().NotBeNull();
        result!.LastLt.Should().Be(OldLt);
        result.LastHash.Should().Be(data.LastHash);
    }

    [Fact]
    public async Task GetCheckpoint_WhenNotFound_ReturnsNull()
    {
        var result = await _repository.GetCheckpointAsync(Chain, Address, Provider);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCheckpoint_WhenExists_UpdatesCheckpoint()
    {
        var data = ObjectMother.Checkpoints.WithConsistentLtAndHash(OldLt).Build();
        await _repository.UpdateCheckpointMonotonicAsync(
            data.Chain, data.Address, data.Provider, OldLt, OldHash);

        await _repository.UpdateCheckpointMonotonicAsync(
            data.Chain, data.Address, data.Provider, NewLt, NewHash);

        var result = await _repository.GetCheckpointAsync(
            data.Chain, data.Address, data.Provider);
        result!.LastLt.Should().Be(NewLt);
        result.LastHash.Should().Be(NewHash);
    }

    [Fact]
    public async Task UpdateCheckpoint_WhenNotFound_CreatesCheckpoint()
    {
        await _repository.UpdateCheckpointMonotonicAsync(Chain, Address, Provider, NewLt, NewHash);

        var result = await _repository.GetCheckpointAsync(Chain, Address, Provider);
        result.Should().NotBeNull();
        result!.LastLt.Should().Be(NewLt);
        result.LastHash.Should().Be(NewHash);
    }
}