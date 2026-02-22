using Vers.Schemes;
using Xunit;

namespace Vers.Tests;

public class SemverVersionComparerTests
{
    private readonly SemverVersionComparer _comparer = SemverVersionComparer.Instance;

    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "1.1.0", -1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)]
    [InlineData("1.0.0-alpha", "1.0.0", -1)]
    [InlineData("1.0.0", "1.0.0-alpha", 1)]
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.2", -1)]
    [InlineData("1.0.0-1", "1.0.0-2", -1)]
    [InlineData("1.0.0-alpha", "1.0.0-alpha.1", -1)]
    [InlineData("1.0.0+build1", "1.0.0+build2", 0)] // build metadata ignored
    public void Compare_FollowsSemver(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        Assert.Equal(expectedSign, result == 0 ? 0 : (result > 0 ? 1 : -1));
    }

    [Theory]
    [InlineData("v1.0.0", "1.0.0", 0)]
    public void Compare_ToleratesLeadingV(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        Assert.Equal(expectedSign, result == 0 ? 0 : (result > 0 ? 1 : -1));
    }

    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("1.0.0-alpha", true)]
    [InlineData("1.0.0+build", true)]
    [InlineData("not-a-version", false)]
    public void IsValid(string version, bool expected)
    {
        Assert.Equal(expected, _comparer.IsValid(version));
    }
}
