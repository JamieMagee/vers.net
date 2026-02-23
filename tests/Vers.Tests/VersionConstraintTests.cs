using Vers.Schemes;

namespace Vers.Tests;

public class VersionConstraintTests
{
    [Test]
    [Arguments(">=1.2.3", Comparator.GreaterThanOrEqual, "1.2.3")]
    [Arguments("<=1.2.3", Comparator.LessThanOrEqual, "1.2.3")]
    [Arguments("!=1.2.3", Comparator.NotEqual, "1.2.3")]
    [Arguments("<1.2.3", Comparator.LessThan, "1.2.3")]
    [Arguments(">1.2.3", Comparator.GreaterThan, "1.2.3")]
    [Arguments("1.2.3", Comparator.Equal, "1.2.3")]
    [Arguments("*", Comparator.Wildcard, "")]
    public async Task Parse_ParsesCorrectly(
        string input,
        Comparator expectedComp,
        string expectedVersion
    )
    {
        var constraint = VersionConstraint.Parse(input);
        await Assert.That(constraint.Comparator).IsEqualTo(expectedComp);
        await Assert.That(constraint.Version).IsEqualTo(expectedVersion);
    }

    [Test]
    public async Task Parse_ThrowsOnEmpty()
    {
        var ex = Assert.Throws<VersException>(() => VersionConstraint.Parse(""));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_ThrowsOnComparatorOnly()
    {
        var ex = Assert.Throws<VersException>(() => VersionConstraint.Parse(">="));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_WildcardWithTrailingChars_Throws()
    {
        var ex = Assert.Throws<VersException>(() => VersionConstraint.Parse("*foo"));
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Parse_ExplicitEquals_StripsPrefix()
    {
        var c = VersionConstraint.Parse("=1.2.3");
        await Assert.That(c.Comparator).IsEqualTo(Comparator.Equal);
        await Assert.That(c.Version).IsEqualTo("1.2.3");
    }

    [Test]
    public async Task UrlEncode_EncodesPercent()
    {
        var c = new VersionConstraint(Comparator.Equal, "1.0%2B1");
        await Assert.That(c.ToString()).IsEqualTo("1.0%252B1");
    }

    [Test]
    [Arguments(">=1.2.3", ">=1.2.3")]
    [Arguments("1.2.3", "1.2.3")]
    [Arguments("*", "*")]
    [Arguments("!=2.0", "!=2.0")]
    public async Task ToString_ProducesCanonicalForm(string input, string expected)
    {
        await Assert.That(VersionConstraint.Parse(input).ToString()).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_UrlDecodesVersion()
    {
        var c = VersionConstraint.Parse(">=%3E1.0");
        await Assert.That(c.Comparator).IsEqualTo(Comparator.GreaterThanOrEqual);
        await Assert.That(c.Version).IsEqualTo(">1.0");
    }

    [Test]
    public async Task Matches_Wildcard_AlwaysTrue()
    {
        var c = new VersionConstraint(Comparator.Wildcard, "");
        await Assert.That(c.Matches("anything", GenericVersionComparer.Instance)).IsTrue();
    }

    [Test]
    [Arguments(Comparator.Equal, "1.0.0", "1.0.0", true)]
    [Arguments(Comparator.Equal, "1.0.0", "2.0.0", false)]
    [Arguments(Comparator.NotEqual, "1.0.0", "2.0.0", true)]
    [Arguments(Comparator.NotEqual, "1.0.0", "1.0.0", false)]
    [Arguments(Comparator.GreaterThan, "1.0.0", "2.0.0", true)]
    [Arguments(Comparator.GreaterThan, "1.0.0", "1.0.0", false)]
    [Arguments(Comparator.GreaterThanOrEqual, "1.0.0", "1.0.0", true)]
    [Arguments(Comparator.LessThan, "2.0.0", "1.0.0", true)]
    [Arguments(Comparator.LessThan, "2.0.0", "2.0.0", false)]
    [Arguments(Comparator.LessThanOrEqual, "2.0.0", "2.0.0", true)]
    public async Task Matches_WithSemver(
        Comparator comparator,
        string constraintVersion,
        string tested,
        bool expected
    )
    {
        var c = new VersionConstraint(comparator, constraintVersion);
        await Assert.That(c.Matches(tested, SemverVersionComparer.Instance)).IsEqualTo(expected);
    }
}
