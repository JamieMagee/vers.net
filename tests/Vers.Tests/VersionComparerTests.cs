using Vers.Schemes;
using Xunit;

namespace Vers.Tests;

public class GenericVersionComparerTests
{
    private readonly GenericVersionComparer _comparer = GenericVersionComparer.Instance;

    [Theory]
    [InlineData("1.0", "1.0", 0)]
    [InlineData("1.0", "2.0", -1)]
    [InlineData("2.0", "1.0", 1)]
    [InlineData("1.2.3", "1.2.4", -1)]
    [InlineData("10.0", "9.0", 1)]
    [InlineData("1.0a", "1.0b", -1)]
    [InlineData("1.0alpha", "1.0beta", -1)]
    public void Compare_Works(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        Assert.Equal(expectedSign, result == 0 ? 0 : (result > 0 ? 1 : -1));
    }
}

public class IntdotVersionComparerTests
{
    private readonly IntdotVersionComparer _comparer = IntdotVersionComparer.Instance;

    [Theory]
    [InlineData("1.0", "1.0", 0)]
    [InlineData("1.0", "2.0", -1)]
    [InlineData("10.234.5.12", "10.234.5.13", -1)]
    [InlineData("10.234.5.12", "10.234.5.12", 0)]
    [InlineData("2", "1.9.9.9", 1)]
    public void Compare_Works(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        Assert.Equal(expectedSign, result == 0 ? 0 : (result > 0 ? 1 : -1));
    }
}

public class LexicographicVersionComparerTests
{
    private readonly LexicographicVersionComparer _comparer = LexicographicVersionComparer.Instance;

    [Theory]
    [InlineData("a", "b", -1)]
    [InlineData("b", "a", 1)]
    [InlineData("abc", "abc", 0)]
    [InlineData("1.0", "2.0", -1)]
    [InlineData("10", "2", -1)] // lexicographic: "1" < "2"
    public void Compare_BytewiseComparison(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        Assert.Equal(expectedSign, result == 0 ? 0 : (result > 0 ? 1 : -1));
    }
}

public class DatetimeVersionComparerTests
{
    private readonly DatetimeVersionComparer _comparer = DatetimeVersionComparer.Instance;

    [Theory]
    [InlineData("2023-01-01T00:00:00Z", "2023-01-02T00:00:00Z", -1)]
    [InlineData("2023-01-01T00:00:00Z", "2023-01-01T00:00:00Z", 0)]
    [InlineData("2023-06-15T12:00:00+05:00", "2023-06-15T07:00:00Z", 0)]
    public void Compare_Works(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        Assert.Equal(expectedSign, result == 0 ? 0 : (result > 0 ? 1 : -1));
    }

    [Theory]
    [InlineData("2023-01-01T00:00:00Z", true)]
    [InlineData("not-a-date", false)]
    public void IsValid(string version, bool expected)
    {
        Assert.Equal(expected, _comparer.IsValid(version));
    }
}
