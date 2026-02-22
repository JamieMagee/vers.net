using Vers.Schemes;
using Xunit;

namespace Vers.Tests;

public class VersionConstraintTests
{
    [Theory]
    [InlineData(">=1.2.3", Comparator.GreaterThanOrEqual, "1.2.3")]
    [InlineData("<=1.2.3", Comparator.LessThanOrEqual, "1.2.3")]
    [InlineData("!=1.2.3", Comparator.NotEqual, "1.2.3")]
    [InlineData("<1.2.3", Comparator.LessThan, "1.2.3")]
    [InlineData(">1.2.3", Comparator.GreaterThan, "1.2.3")]
    [InlineData("1.2.3", Comparator.Equal, "1.2.3")]
    [InlineData("*", Comparator.Wildcard, "")]
    public void Parse_ParsesCorrectly(string input, Comparator expectedComp, string expectedVersion)
    {
        var constraint = VersionConstraint.Parse(input);
        Assert.Equal(expectedComp, constraint.Comparator);
        Assert.Equal(expectedVersion, constraint.Version);
    }

    [Fact]
    public void Parse_ThrowsOnEmpty()
    {
        Assert.Throws<VersException>(() => VersionConstraint.Parse(""));
    }

    [Fact]
    public void Parse_ThrowsOnComparatorOnly()
    {
        Assert.Throws<VersException>(() => VersionConstraint.Parse(">="));
    }

    [Theory]
    [InlineData(">=1.2.3", ">=1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("*", "*")]
    [InlineData("!=2.0", "!=2.0")]
    public void ToString_ProducesCanonicalForm(string input, string expected)
    {
        Assert.Equal(expected, VersionConstraint.Parse(input).ToString());
    }

    [Fact]
    public void Parse_UrlDecodesVersion()
    {
        var c = VersionConstraint.Parse(">=%3E1.0");
        Assert.Equal(Comparator.GreaterThanOrEqual, c.Comparator);
        Assert.Equal(">1.0", c.Version);
    }

    [Fact]
    public void Matches_Wildcard_AlwaysTrue()
    {
        var c = new VersionConstraint(Comparator.Wildcard, "");
        Assert.True(c.Matches("anything", GenericVersionComparer.Instance));
    }

    [Theory]
    [InlineData(Comparator.Equal, "1.0.0", "1.0.0", true)]
    [InlineData(Comparator.Equal, "1.0.0", "2.0.0", false)]
    [InlineData(Comparator.NotEqual, "1.0.0", "2.0.0", true)]
    [InlineData(Comparator.NotEqual, "1.0.0", "1.0.0", false)]
    [InlineData(Comparator.GreaterThan, "1.0.0", "2.0.0", true)]
    [InlineData(Comparator.GreaterThan, "1.0.0", "1.0.0", false)]
    [InlineData(Comparator.GreaterThanOrEqual, "1.0.0", "1.0.0", true)]
    [InlineData(Comparator.LessThan, "2.0.0", "1.0.0", true)]
    [InlineData(Comparator.LessThan, "2.0.0", "2.0.0", false)]
    [InlineData(Comparator.LessThanOrEqual, "2.0.0", "2.0.0", true)]
    public void Matches_WithSemver(
        Comparator comparator,
        string constraintVersion,
        string tested,
        bool expected
    )
    {
        var c = new VersionConstraint(comparator, constraintVersion);
        Assert.Equal(expected, c.Matches(tested, SemverVersionComparer.Instance));
    }
}
