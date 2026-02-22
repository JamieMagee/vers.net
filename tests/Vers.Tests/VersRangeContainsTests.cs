namespace Vers.Tests;

public class VersRangeContainsTests
{
    [Test]
    public async Task Wildcard_ContainsEverything()
    {
        var v = VersRange.Parse("vers:npm/*");
        await Assert.That(v.Contains("0.0.0")).IsTrue();
        await Assert.That(v.Contains("999.999.999")).IsTrue();
        await Assert.That(v.Contains("anything")).IsTrue();
    }

    [Test]
    public async Task ExactVersion_ContainsOnlyThat()
    {
        var v = VersRange.Parse("vers:npm/1.2.3");
        await Assert.That(v.Contains("1.2.3")).IsTrue();
        await Assert.That(v.Contains("1.2.4")).IsFalse();
        await Assert.That(v.Contains("1.2.2")).IsFalse();
    }

    [Test]
    public async Task GreaterThanOrEqual_LessThan_Range()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
        await Assert.That(v.Contains("1.0.0")).IsTrue();
        await Assert.That(v.Contains("2.5.0")).IsTrue();
        await Assert.That(v.Contains("3.0.0")).IsFalse();
        await Assert.That(v.Contains("0.9.0")).IsFalse();
        await Assert.That(v.Contains("3.0.1")).IsFalse();
    }

    [Test]
    public async Task GreaterThan_LessThanOrEqual()
    {
        var v = VersRange.Parse("vers:npm/>1.0.0|<=3.0.0");
        await Assert.That(v.Contains("1.0.0")).IsFalse();
        await Assert.That(v.Contains("1.0.1")).IsTrue();
        await Assert.That(v.Contains("3.0.0")).IsTrue();
        await Assert.That(v.Contains("3.0.1")).IsFalse();
    }

    [Test]
    public async Task NotEqual_Excludes()
    {
        var v = VersRange.Parse("vers:npm/!=1.5.0|>=1.0.0|<2.0.0");
        await Assert.That(v.Contains("1.0.0")).IsTrue();
        await Assert.That(v.Contains("1.9.0")).IsTrue();
        await Assert.That(v.Contains("1.5.0")).IsFalse();
        await Assert.That(v.Contains("0.9.0")).IsFalse();
    }

    [Test]
    public async Task MultipleEqualities()
    {
        var v = VersRange.Parse("vers:pypi/1.0|2.0|3.0");
        await Assert.That(v.Contains("1.0")).IsTrue();
        await Assert.That(v.Contains("2.0")).IsTrue();
        await Assert.That(v.Contains("3.0")).IsTrue();
        await Assert.That(v.Contains("4.0")).IsFalse();
        await Assert.That(v.Contains("1.5")).IsFalse();
    }

    [Test]
    public async Task DisjointRanges()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0");
        await Assert.That(v.Contains("1.0.0")).IsTrue();
        await Assert.That(v.Contains("1.3.0")).IsTrue();
        await Assert.That(v.Contains("1.5.0")).IsTrue();
        await Assert.That(v.Contains("2.0.0")).IsFalse();
        await Assert.That(v.Contains("3.0.0")).IsTrue();
        await Assert.That(v.Contains("3.5.0")).IsTrue();
        await Assert.That(v.Contains("4.0.0")).IsTrue();
        await Assert.That(v.Contains("5.0.0")).IsFalse();
    }

    [Test]
    public async Task OpenEndedGreater()
    {
        var v = VersRange.Parse("vers:npm/>=2.0.0");
        await Assert.That(v.Contains("2.0.0")).IsTrue();
        await Assert.That(v.Contains("99.0.0")).IsTrue();
        await Assert.That(v.Contains("1.9.9")).IsFalse();
    }

    [Test]
    public async Task OpenEndedLesser()
    {
        var v = VersRange.Parse("vers:npm/<2.0.0");
        await Assert.That(v.Contains("1.9.9")).IsTrue();
        await Assert.That(v.Contains("0.0.1")).IsTrue();
        await Assert.That(v.Contains("2.0.0")).IsFalse();
        await Assert.That(v.Contains("3.0.0")).IsFalse();
    }

    [Test]
    public async Task SemverPrerelease()
    {
        var v = VersRange.Parse("vers:npm/>=1.0.0-alpha|<1.0.0");
        await Assert.That(v.Contains("1.0.0-beta")).IsTrue();
        await Assert.That(v.Contains("1.0.0-alpha")).IsTrue();
        await Assert.That(v.Contains("1.0.0")).IsFalse();
    }

    [Test]
    public async Task AllScheme_ContainsEverything()
    {
        var v = VersRange.Parse("vers:all/*");
        await Assert.That(v.Contains("anything")).IsTrue();
        await Assert.That(v.Contains("1.0.0")).IsTrue();
    }

    [Test]
    public async Task NoneScheme_ContainsNothing()
    {
        var v = VersRange.Parse("vers:none/*");
        await Assert.That(v.Contains("anything")).IsFalse();
        await Assert.That(v.Contains("1.0.0")).IsFalse();
    }
}
