using Xunit;

namespace Vers.Tests;

public class VersRangeContainsTests
{
    [Fact]
    public void Wildcard_ContainsEverything()
    {
        var v = VersRange.Parse("vers:npm/*");
        Assert.True(v.Contains("0.0.0"));
        Assert.True(v.Contains("999.999.999"));
        Assert.True(v.Contains("anything"));
    }

    [Fact]
    public void ExactVersion_ContainsOnlyThat()
    {
        var v = VersRange.Parse("vers:npm/1.2.3");
        Assert.True(v.Contains("1.2.3"));
        Assert.False(v.Contains("1.2.4"));
        Assert.False(v.Contains("1.2.2"));
    }

    [Fact]
    public void GreaterThanOrEqual_LessThan_Range()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
        Assert.True(v.Contains("1.0.0"));
        Assert.True(v.Contains("2.5.0"));
        Assert.False(v.Contains("3.0.0"));
        Assert.False(v.Contains("0.9.0"));
        Assert.False(v.Contains("3.0.1"));
    }

    [Fact]
    public void GreaterThan_LessThanOrEqual()
    {
        var v = VersRange.Parse("vers:npm/>1.0.0|<=3.0.0");
        Assert.False(v.Contains("1.0.0"));
        Assert.True(v.Contains("1.0.1"));
        Assert.True(v.Contains("3.0.0"));
        Assert.False(v.Contains("3.0.1"));
    }

    [Fact]
    public void NotEqual_Excludes()
    {
        var v = VersRange.Parse("vers:npm/!=1.5.0|>=1.0.0|<2.0.0");
        Assert.True(v.Contains("1.0.0"));
        Assert.True(v.Contains("1.9.0"));
        Assert.False(v.Contains("1.5.0"));
        Assert.False(v.Contains("0.9.0"));
    }

    [Fact]
    public void MultipleEqualities()
    {
        var v = VersRange.Parse("vers:pypi/1.0|2.0|3.0");
        Assert.True(v.Contains("1.0"));
        Assert.True(v.Contains("2.0"));
        Assert.True(v.Contains("3.0"));
        Assert.False(v.Contains("4.0"));
        Assert.False(v.Contains("1.5"));
    }

    [Fact]
    public void DisjointRanges()
    {
        // >=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0 means: [1.0, 1.5] OR [3.0, 4.0]
        var v = VersRange.Parse("vers:npm/>=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0");
        Assert.True(v.Contains("1.0.0"));
        Assert.True(v.Contains("1.3.0"));
        Assert.True(v.Contains("1.5.0"));
        Assert.False(v.Contains("2.0.0"));
        Assert.True(v.Contains("3.0.0"));
        Assert.True(v.Contains("3.5.0"));
        Assert.True(v.Contains("4.0.0"));
        Assert.False(v.Contains("5.0.0"));
    }

    [Fact]
    public void OpenEndedGreater()
    {
        // >=2.0.0 means anything >= 2.0
        var v = VersRange.Parse("vers:npm/>=2.0.0");
        Assert.True(v.Contains("2.0.0"));
        Assert.True(v.Contains("99.0.0"));
        Assert.False(v.Contains("1.9.9"));
    }

    [Fact]
    public void OpenEndedLesser()
    {
        // <2.0.0 means anything < 2.0
        var v = VersRange.Parse("vers:npm/<2.0.0");
        Assert.True(v.Contains("1.9.9"));
        Assert.True(v.Contains("0.0.1"));
        Assert.False(v.Contains("2.0.0"));
        Assert.False(v.Contains("3.0.0"));
    }

    [Fact]
    public void SemverPrerelease()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0-alpha|<1.0.0");
        Assert.True(v.Contains("1.0.0-beta"));
        Assert.True(v.Contains("1.0.0-alpha"));
        Assert.False(v.Contains("1.0.0"));
    }

    [Fact]
    public void AllScheme_ContainsEverything()
    {
        var v = VersRange.Parse("vers:all/*");
        Assert.True(v.Contains("anything"));
        Assert.True(v.Contains("1.0.0"));
    }

    [Fact]
    public void NoneScheme_ContainsNothing()
    {
        var v = VersRange.Parse("vers:none/*");
        Assert.False(v.Contains("anything"));
        Assert.False(v.Contains("1.0.0"));
    }
}
