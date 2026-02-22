namespace Vers.Tests;

public class ComparatorTests
{
    [Test]
    [Arguments(Comparator.LessThan, "<")]
    [Arguments(Comparator.LessThanOrEqual, "<=")]
    [Arguments(Comparator.Equal, "=")]
    [Arguments(Comparator.NotEqual, "!=")]
    [Arguments(Comparator.GreaterThan, ">")]
    [Arguments(Comparator.GreaterThanOrEqual, ">=")]
    [Arguments(Comparator.Wildcard, "*")]
    public async Task ToSymbol_ReturnsCorrectSymbol(Comparator comparator, string expected)
    {
        await Assert.That(comparator.ToSymbol()).IsEqualTo(expected);
    }

    [Test]
    [Arguments(">=1.0", Comparator.GreaterThanOrEqual, 2)]
    [Arguments("<=1.0", Comparator.LessThanOrEqual, 2)]
    [Arguments("!=1.0", Comparator.NotEqual, 2)]
    [Arguments("<1.0", Comparator.LessThan, 1)]
    [Arguments(">1.0", Comparator.GreaterThan, 1)]
    [Arguments("*", Comparator.Wildcard, 1)]
    public async Task TryParsePrefix_ParsesCorrectly(
        string input,
        Comparator expected,
        int expectedLength
    )
    {
        var success = ComparatorExtensions.TryParsePrefix(
            input,
            0,
            out var comparator,
            out var length
        );
        await Assert.That(success).IsTrue();
        await Assert.That(comparator).IsEqualTo(expected);
        await Assert.That(length).IsEqualTo(expectedLength);
    }

    [Test]
    [Arguments("1.0")]
    [Arguments("abc")]
    public async Task TryParsePrefix_ReturnsFalse_WhenNoComparator(string input)
    {
        var success = ComparatorExtensions.TryParsePrefix(input, 0, out _, out _);
        await Assert.That(success).IsFalse();
    }
}
