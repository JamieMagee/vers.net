using Xunit;

namespace Vers.Tests;

public class ComparatorTests
{
    [Theory]
    [InlineData(Comparator.LessThan, "<")]
    [InlineData(Comparator.LessThanOrEqual, "<=")]
    [InlineData(Comparator.Equal, "=")]
    [InlineData(Comparator.NotEqual, "!=")]
    [InlineData(Comparator.GreaterThan, ">")]
    [InlineData(Comparator.GreaterThanOrEqual, ">=")]
    [InlineData(Comparator.Wildcard, "*")]
    public void ToSymbol_ReturnsCorrectSymbol(Comparator comparator, string expected)
    {
        Assert.Equal(expected, comparator.ToSymbol());
    }

    [Theory]
    [InlineData(">=1.0", Comparator.GreaterThanOrEqual, 2)]
    [InlineData("<=1.0", Comparator.LessThanOrEqual, 2)]
    [InlineData("!=1.0", Comparator.NotEqual, 2)]
    [InlineData("<1.0", Comparator.LessThan, 1)]
    [InlineData(">1.0", Comparator.GreaterThan, 1)]
    [InlineData("*", Comparator.Wildcard, 1)]
    public void TryParsePrefix_ParsesCorrectly(
        string input,
        Comparator expected,
        int expectedLength
    )
    {
        Assert.True(
            ComparatorExtensions.TryParsePrefix(input, 0, out var comparator, out var length)
        );
        Assert.Equal(expected, comparator);
        Assert.Equal(expectedLength, length);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("abc")]
    public void TryParsePrefix_ReturnsFalse_WhenNoComparator(string input)
    {
        Assert.False(ComparatorExtensions.TryParsePrefix(input, 0, out _, out _));
    }
}
