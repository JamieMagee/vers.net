namespace Vers.Tests;

public class VersRangeValidateTests
{
    [Test]
    public async Task Validate_WildcardAlone_IsValid()
    {
        var v = VersRange.Parse("vers:npm/*");
        v.Validate();
        await Task.CompletedTask;
    }

    [Test]
    public async Task Validate_SortedConstraints_IsValid()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
        v.Validate();
        await Task.CompletedTask;
    }

    [Test]
    public async Task Validate_DisjointRanges_IsValid()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0");
        v.Validate();
        await Task.CompletedTask;
    }

    [Test]
    public async Task Builder_SortsAndValidates()
    {
        var v = new VersRange.Builder("npm")
            .AddConstraint(Comparator.LessThan, "3.0.0")
            .AddConstraint(Comparator.GreaterThanOrEqual, "1.0.0")
            .Build();

        await Assert.That(v.ToString()).IsEqualTo("vers:npm/>=1.0.0|<3.0.0");
    }

    [Test]
    public async Task Builder_Wildcard()
    {
        var v = new VersRange.Builder("deb").AddWildcard().Build();

        await Assert.That(v.ToString()).IsEqualTo("vers:deb/*");
    }

    [Test]
    public async Task Builder_ThrowsOnNoConstraints()
    {
        var ex = Assert.Throws<VersException>(() => new VersRange.Builder("npm").Build());
        await Assert.That(ex).IsNotNull();
    }
}
