using Vers.Schemes;

namespace Vers.Tests;

public class GenericVersionComparerTests
{
    private readonly GenericVersionComparer _comparer = GenericVersionComparer.Instance;

    [Test]
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("2.0", "1.0", 1)]
    [Arguments("1.2.3", "1.2.4", -1)]
    [Arguments("10.0", "9.0", 1)]
    [Arguments("1.0a", "1.0b", -1)]
    [Arguments("1.0alpha", "1.0beta", -1)]
    public async Task Compare_Works(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}

public class IntdotVersionComparerTests
{
    private readonly IntdotVersionComparer _comparer = IntdotVersionComparer.Instance;

    [Test]
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("10.234.5.12", "10.234.5.13", -1)]
    [Arguments("10.234.5.12", "10.234.5.12", 0)]
    [Arguments("2", "1.9.9.9", 1)]
    public async Task Compare_Works(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}

public class LexicographicVersionComparerTests
{
    private readonly LexicographicVersionComparer _comparer = LexicographicVersionComparer.Instance;

    [Test]
    [Arguments("a", "b", -1)]
    [Arguments("b", "a", 1)]
    [Arguments("abc", "abc", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("10", "2", -1)]
    public async Task Compare_BytewiseComparison(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}

public class DatetimeVersionComparerTests
{
    private readonly DatetimeVersionComparer _comparer = DatetimeVersionComparer.Instance;

    [Test]
    [Arguments("2023-01-01T00:00:00Z", "2023-01-02T00:00:00Z", -1)]
    [Arguments("2023-01-01T00:00:00Z", "2023-01-01T00:00:00Z", 0)]
    [Arguments("2023-06-15T12:00:00+05:00", "2023-06-15T07:00:00Z", 0)]
    public async Task Compare_Works(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    [Test]
    [Arguments("2023-01-01T00:00:00Z", true)]
    [Arguments("not-a-date", false)]
    public async Task IsValid(string version, bool expected)
    {
        await Assert.That(_comparer.IsValid(version)).IsEqualTo(expected);
    }
}
