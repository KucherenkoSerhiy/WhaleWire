using FluentAssertions;
using WhaleWire.Domain.Services;
using Xunit;

namespace WhaleWire.Tests.Common.Builders;

public class EventIdGeneratorTests
{
    [Fact]
    public void EventIdBuilder_SameInputs_ProducesSameId()
    {
        // Arrange
        const string chain = "ton";
        const string address = "EQTest";
        const long lt = 123;
        const string txHash = "hash123";

        // Act
        var id1 = EventIdGenerator.Generate(chain, address, lt, txHash);
        var id2 = EventIdGenerator.Generate(chain, address, lt, txHash);

        // Assert
        id1.Should().Be(id2);
        id1.Should().HaveLength(16);
    }
    
    [Fact]
    public void EventIdBuilder_DifferentInputs_ProducesDifferentIds()
    {
        var id1 = EventIdGenerator.Generate("ton", "addr1", 100, "hash1");
        var id2 = EventIdGenerator.Generate("ton", "addr2", 100, "hash1");
       
        id1.Should().NotBe(id2);
    }
}