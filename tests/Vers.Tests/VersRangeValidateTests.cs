using Vers.Schemes;
using Xunit;

namespace Vers.Tests;

public class VersRangeValidateTests
{
    [Fact]
    public void Validate_WildcardAlone_IsValid()
    {
        var v = VersRange.Parse("vers:npm/*");
        v.Validate(); // should not throw
    }

    [Fact]
    public void Validate_SortedConstraints_IsValid()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
        v.Validate(); // should not throw
    }

    [Fact]
    public void Validate_DisjointRanges_IsValid()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0");
        v.Validate(); // should not throw
    }

    [Fact]
    public void Builder_SortsAndValidates()
    {
        var v = new VersRange.Builder("npm")
            .AddConstraint(Comparator.LessThan, "3.0.0")
            .AddConstraint(Comparator.GreaterThanOrEqual, "1.0.0")
            .Build();

        Assert.Equal("vers:npm/>=1.0.0|<3.0.0", v.ToString());
    }

    [Fact]
    public void Builder_Wildcard()
    {
        var v = new VersRange.Builder("deb").AddWildcard().Build();

        Assert.Equal("vers:deb/*", v.ToString());
    }

    [Fact]
    public void Builder_ThrowsOnNoConstraints()
    {
        Assert.Throws<VersException>(() => new VersRange.Builder("npm").Build());
    }
}
