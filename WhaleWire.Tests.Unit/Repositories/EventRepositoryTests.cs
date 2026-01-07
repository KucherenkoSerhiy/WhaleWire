using AutoFixture;
using FluentAssertions;
using WhaleWire.Infrastructure.Persistence.Repositories;
using WhaleWire.Tests.Common.Builders;
using WhaleWire.Tests.Common.Fixtures;

namespace WhaleWire.Tests.Unit.Repositories;

public sealed class EventRepositoryTests : InMemoryDbContextFixture
{
    private readonly EventRepository _repository;
    private readonly Fixture _fixture = new();

    public EventRepositoryTests()
    {
        _repository = new EventRepository(Context);
    }

    [Fact]
    public async Task UpsertEventIdempotent_FirstInsert_ReturnsTrue()
    {
        // Arrange
        var eventData = ObjectMother.Events.Default().Build();

        // Act
        var result = await _repository.UpsertEventIdempotentAsync(
            eventData.EventId, eventData.Chain, eventData.Address,
            eventData.Lt, eventData.TxHash, eventData.BlockTime, eventData.RawJson);

        // Assert
        result.Should().BeTrue();
        Context.Events.Should().ContainSingle();
    }

    [Fact]
    public async Task UpsertEventIdempotent_DuplicateEventId_ReturnsFalse()
    {
        // Arrange
        const string knownEventId = "duplicate-test-event";
        var firstEvent = ObjectMother.Events.WithKnownEventId(knownEventId).Build();
        var differentLt = _fixture.Create<long>();
        var secondEvent = ObjectMother.Events
            .WithKnownEventId(knownEventId)
            .WithLt(differentLt)
            .Build();

        await InsertEventAsync(firstEvent);

        // Act
        var result = await InsertEventAsync(secondEvent);

        // Assert
        result.Should().BeFalse();
        Context.Events.Should().ContainSingle();
    }

    private Task<bool> InsertEventAsync(EventTestData data) =>
        _repository.UpsertEventIdempotentAsync(
            data.EventId, data.Chain, data.Address, data.Lt,
            data.TxHash, data.BlockTime, data.RawJson);
}