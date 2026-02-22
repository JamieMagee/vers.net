using Xunit;

namespace Vers.Tests;

public class VersRangeParseTests
{
    [Fact]
    public void Parse_SimpleEquality()
    {
        var v = VersRange.Parse("vers:npm/1.2.3");
        Assert.Equal("npm", v.Scheme);
        Assert.Single(v.Constraints);
        Assert.Equal(Comparator.Equal, v.Constraints[0].Comparator);
        Assert.Equal("1.2.3", v.Constraints[0].Version);
    }

    [Fact]
    public void Parse_MultipleConstraints()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
        Assert.Equal("npm", v.Scheme);
        Assert.Equal(2, v.Constraints.Count);
        Assert.Equal(Comparator.GreaterThanOrEqual, v.Constraints[0].Comparator);
        Assert.Equal("1.0.0", v.Constraints[0].Version);
        Assert.Equal(Comparator.LessThan, v.Constraints[1].Comparator);
        Assert.Equal("3.0.0", v.Constraints[1].Version);
    }

    [Fact]
    public void Parse_Wildcard()
    {
        var v = VersRange.Parse("vers:deb/*");
        Assert.Equal("deb", v.Scheme);
        Assert.Single(v.Constraints);
        Assert.Equal(Comparator.Wildcard, v.Constraints[0].Comparator);
    }

    [Fact]
    public void Parse_EnumeratedVersions()
    {
        var v = VersRange.Parse("vers:pypi/0.0.0|0.0.1|0.0.2|1.0|2.0pre1");
        Assert.Equal("pypi", v.Scheme);
        Assert.Equal(5, v.Constraints.Count);
        Assert.All(v.Constraints, c => Assert.Equal(Comparator.Equal, c.Comparator));
    }

    [Fact]
    public void Parse_IgnoresSpaces()
    {
        var v = VersRange.Parse("vers : npm / >= 1.0.0 | < 3.0.0");
        Assert.Equal("npm", v.Scheme);
        Assert.Equal(2, v.Constraints.Count);
    }

    [Fact]
    public void Parse_SchemeIsLowercase()
    {
        var v = VersRange.Parse("vers:NPM/1.0.0");
        Assert.Equal("npm", v.Scheme);
    }

    [Fact]
    public void Parse_ThrowsOnMissingColon()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("versnpm/1.0.0"));
    }

    [Fact]
    public void Parse_ThrowsOnWrongScheme()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("purl:npm/1.0.0"));
    }

    [Fact]
    public void Parse_ThrowsOnMissingSlash()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("vers:npm"));
    }

    [Fact]
    public void Parse_ThrowsOnEmptyConstraints()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("vers:npm/"));
    }

    [Fact]
    public void Parse_MavenComplexRange()
    {
        var v = VersRange.Parse(
            "vers:maven/>=1.0.0-beta1|<=1.7.5|>=7.0.0-M1|<=7.0.7|>=7.1.0|<=7.1.2|>=8.0.0-M1|<=8.0.1"
        );
        Assert.Equal("maven", v.Scheme);
        Assert.Equal(8, v.Constraints.Count);
    }

    [Fact]
    public void ToString_RoundTrips()
    {
        var input = "vers:npm/>=1.0.0|<3.0.0";
        var v = VersRange.Parse(input);
        Assert.Equal(input, v.ToString());
    }

    [Fact]
    public void ToString_Wildcard()
    {
        Assert.Equal("vers:deb/*", VersRange.Parse("vers:deb/*").ToString());
    }

    [Fact]
    public void ToString_EqualityOmitsComparator()
    {
        Assert.Equal("vers:npm/1.2.3", VersRange.Parse("vers:npm/1.2.3").ToString());
    }

    [Fact]
    public void Parse_NoneScheme_OnlyWildcard()
    {
        var v = VersRange.Parse("vers:none/*");
        Assert.Equal("none", v.Scheme);
        Assert.Single(v.Constraints);
        Assert.Equal(Comparator.Wildcard, v.Constraints[0].Comparator);
    }

    [Fact]
    public void Parse_AllScheme_OnlyWildcard()
    {
        var v = VersRange.Parse("vers:all/*");
        Assert.Equal("all", v.Scheme);
    }

    [Fact]
    public void Parse_NoneScheme_RejectsNonWildcard()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("vers:none/1.0"));
        Assert.Throws<VersException>(() => VersRange.Parse("vers:none/>=1.0"));
    }

    [Fact]
    public void Parse_AllScheme_RejectsNonWildcard()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("vers:all/1.0"));
        Assert.Throws<VersException>(() => VersRange.Parse("vers:all/>=1.0"));
    }
}
