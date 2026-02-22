namespace Vers.Tests;

public class VersRangeParseTests
{
    [Test]
    public async Task Parse_SimpleEquality()
    {
        var v = VersRange.Parse("vers:npm/1.2.3");
        await Assert.That(v.Scheme).IsEqualTo("npm");
        await Assert.That(v.Constraints).Count().IsEqualTo(1);
        await Assert.That(v.Constraints[0].Comparator).IsEqualTo(Comparator.Equal);
        await Assert.That(v.Constraints[0].Version).IsEqualTo("1.2.3");
    }

    [Test]
    public async Task Parse_MultipleConstraints()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
        await Assert.That(v.Scheme).IsEqualTo("npm");
        await Assert.That(v.Constraints).Count().IsEqualTo(2);
        await Assert.That(v.Constraints[0].Comparator).IsEqualTo(Comparator.GreaterThanOrEqual);
        await Assert.That(v.Constraints[0].Version).IsEqualTo("1.0.0");
        await Assert.That(v.Constraints[1].Comparator).IsEqualTo(Comparator.LessThan);
        await Assert.That(v.Constraints[1].Version).IsEqualTo("3.0.0");
    }

    [Test]
    public async Task Parse_Wildcard()
    {
        var v = VersRange.Parse("vers:deb/*");
        await Assert.That(v.Scheme).IsEqualTo("deb");
        await Assert.That(v.Constraints).Count().IsEqualTo(1);
        await Assert.That(v.Constraints[0].Comparator).IsEqualTo(Comparator.Wildcard);
    }

    [Test]
    public async Task Parse_EnumeratedVersions()
    {
        var v = VersRange.Parse("vers:pypi/0.0.0|0.0.1|0.0.2|1.0|2.0pre1");
        await Assert.That(v.Scheme).IsEqualTo("pypi");
        await Assert.That(v.Constraints).Count().IsEqualTo(5);
    }

    [Test]
    public async Task Parse_IgnoresSpaces()
    {
        var v = VersRange.Parse("vers : npm / >= 1.0.0 | < 3.0.0");
        await Assert.That(v.Scheme).IsEqualTo("npm");
        await Assert.That(v.Constraints).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Parse_SchemeIsLowercase()
    {
        var v = VersRange.Parse("vers:NPM/1.0.0");
        await Assert.That(v.Scheme).IsEqualTo("npm");
    }

    [Test]
    public async Task Parse_ThrowsOnMissingColon()
    {
        var ex = Assert.Throws<VersException>(() => VersRange.Parse("versnpm/1.0.0"));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_ThrowsOnWrongScheme()
    {
        var ex = Assert.Throws<VersException>(() => VersRange.Parse("purl:npm/1.0.0"));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_ThrowsOnMissingSlash()
    {
        var ex = Assert.Throws<VersException>(() => VersRange.Parse("vers:npm"));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_ThrowsOnEmptyConstraints()
    {
        var ex = Assert.Throws<VersException>(() => VersRange.Parse("vers:npm/"));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_MavenComplexRange()
    {
        var v = VersRange.Parse(
            "vers:maven/>=1.0.0-beta1|<=1.7.5|>=7.0.0-M1|<=7.0.7|>=7.1.0|<=7.1.2|>=8.0.0-M1|<=8.0.1"
        );
        await Assert.That(v.Scheme).IsEqualTo("maven");
        await Assert.That(v.Constraints).Count().IsEqualTo(8);
    }

    [Test]
    public async Task ToString_RoundTrips()
    {
        var input = "vers:npm/>=1.0.0|<3.0.0";
        var v = VersRange.Parse(input);
        await Assert.That(v.ToString()).IsEqualTo(input);
    }

    [Test]
    public async Task ToString_Wildcard()
    {
        await Assert.That(VersRange.Parse("vers:deb/*").ToString()).IsEqualTo("vers:deb/*");
    }

    [Test]
    public async Task ToString_EqualityOmitsComparator()
    {
        await Assert.That(VersRange.Parse("vers:npm/1.2.3").ToString()).IsEqualTo("vers:npm/1.2.3");
    }

    [Test]
    public async Task Parse_NoneScheme_OnlyWildcard()
    {
        var v = VersRange.Parse("vers:none/*");
        await Assert.That(v.Scheme).IsEqualTo("none");
        await Assert.That(v.Constraints).Count().IsEqualTo(1);
        await Assert.That(v.Constraints[0].Comparator).IsEqualTo(Comparator.Wildcard);
    }

    [Test]
    public async Task Parse_AllScheme_OnlyWildcard()
    {
        var v = VersRange.Parse("vers:all/*");
        await Assert.That(v.Scheme).IsEqualTo("all");
    }

    [Test]
    public async Task Parse_NoneScheme_RejectsNonWildcard()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("vers:none/1.0"));
        Assert.Throws<VersException>(() => VersRange.Parse("vers:none/>=1.0"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Parse_AllScheme_RejectsNonWildcard()
    {
        Assert.Throws<VersException>(() => VersRange.Parse("vers:all/1.0"));
        Assert.Throws<VersException>(() => VersRange.Parse("vers:all/>=1.0"));
        await Task.CompletedTask;
    }
}
