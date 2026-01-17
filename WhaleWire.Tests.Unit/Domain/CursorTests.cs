using FluentAssertions;
using WhaleWire.Domain;

namespace WhaleWire.Tests.Unit.Domain;

public sealed class CursorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhiteSpaceInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => Cursor.Parse(input));
    }
    
    [Theory]
    [InlineData("12345")]
    [InlineData("12345:")]
    public void Parse_MissingSecondary_ReturnsFalse(string input)
    {
        Assert.Throws<FormatException>(() => Cursor.Parse(input));
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Cursor.Parse("not-a-number:abc123"));
    }

    [Fact]
    public void Parse_ValidString_ReturnsNewCursor()
    {
        var input = "12345:abc123";
        var cursor = Cursor.Parse(input);

        cursor.Primary.Should().Be(12345);
        cursor.Secondary.Should().Be("abc123");
    }
    
    [Fact]
    public void ToString_AlwaysReturnsFullFormat()
    {
        var cursor = new Cursor(12345, "abc123");
        var result = cursor.ToString();
        result.Should().Be("12345:abc123");
    }
}
