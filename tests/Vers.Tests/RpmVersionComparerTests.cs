using Vers.Schemes;

namespace Vers.Tests;

/// <summary>
/// Test cases derived from the canonical RPM rpmvercmp test suite at
/// https://github.com/rpm-software-management/rpm/blob/rpm-4.20.x/tests/rpmvercmp.at
/// </summary>
public class RpmVersionComparerTests
{
    private readonly RpmVersionComparer _comparer = RpmVersionComparer.Instance;

    // Basic numeric comparison
    [Test]
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("2.0", "1.0", 1)]
    // Multi-segment
    [Arguments("2.0.1", "2.0.1", 0)]
    [Arguments("2.0", "2.0.1", -1)]
    [Arguments("2.0.1", "2.0", 1)]
    // Alpha suffix
    [Arguments("2.0.1a", "2.0.1a", 0)]
    [Arguments("2.0.1a", "2.0.1", 1)]
    [Arguments("2.0.1", "2.0.1a", -1)]
    // p-suffix (treated as alpha segment)
    [Arguments("5.5p1", "5.5p1", 0)]
    [Arguments("5.5p1", "5.5p2", -1)]
    [Arguments("5.5p2", "5.5p1", 1)]
    [Arguments("5.5p10", "5.5p10", 0)]
    [Arguments("5.5p1", "5.5p10", -1)]
    [Arguments("5.5p10", "5.5p1", 1)]
    // Mixed alpha-numeric
    [Arguments("10xyz", "10.1xyz", -1)]
    [Arguments("10.1xyz", "10xyz", 1)]
    [Arguments("xyz10", "xyz10", 0)]
    [Arguments("xyz10", "xyz10.1", -1)]
    [Arguments("xyz10.1", "xyz10", 1)]
    // Alpha vs numeric: numeric wins
    [Arguments("xyz.4", "8", -1)]
    [Arguments("8", "xyz.4", 1)]
    [Arguments("xyz.4", "2", -1)]
    [Arguments("2", "xyz.4", 1)]
    // Cross-segment comparison
    [Arguments("5.5p2", "5.6p1", -1)]
    [Arguments("5.6p1", "5.5p2", 1)]
    [Arguments("5.6p1", "6.5p1", -1)]
    [Arguments("6.5p1", "5.6p1", 1)]
    // rc suffix
    [Arguments("6.0.rc1", "6.0", 1)]
    [Arguments("6.0", "6.0.rc1", -1)]
    // Alpha ordering
    [Arguments("10b2", "10a1", 1)]
    [Arguments("10a2", "10b2", -1)]
    [Arguments("1.0aa", "1.0aa", 0)]
    [Arguments("1.0a", "1.0aa", -1)]
    [Arguments("1.0aa", "1.0a", 1)]
    // Leading zeros
    [Arguments("10.0001", "10.0001", 0)]
    [Arguments("10.0001", "10.1", 0)]
    [Arguments("10.1", "10.0001", 0)]
    [Arguments("10.0001", "10.0039", -1)]
    [Arguments("10.0039", "10.0001", 1)]
    // Large numbers
    [Arguments("4.999.9", "5.0", -1)]
    [Arguments("5.0", "4.999.9", 1)]
    // Date-like versions
    [Arguments("20101121", "20101121", 0)]
    [Arguments("20101121", "20101122", -1)]
    [Arguments("20101122", "20101121", 1)]
    // Separator equivalence
    [Arguments("2_0", "2_0", 0)]
    [Arguments("2.0", "2_0", 0)]
    [Arguments("2_0", "2.0", 0)]
    // Pure alpha
    [Arguments("a", "a", 0)]
    [Arguments("a+", "a+", 0)]
    [Arguments("a+", "a_", 0)]
    [Arguments("a_", "a+", 0)]
    [Arguments("+a", "+a", 0)]
    [Arguments("+a", "_a", 0)]
    [Arguments("_a", "+a", 0)]
    [Arguments("+", "_", 0)]
    [Arguments("_", "+", 0)]
    public async Task Compare_CanonicalCases(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Tilde sorting
    [Test]
    [Arguments("1.0~rc1", "1.0~rc1", 0)]
    [Arguments("1.0~rc1", "1.0", -1)]
    [Arguments("1.0", "1.0~rc1", 1)]
    [Arguments("1.0~rc1", "1.0~rc2", -1)]
    [Arguments("1.0~rc2", "1.0~rc1", 1)]
    [Arguments("1.0~rc1~git123", "1.0~rc1~git123", 0)]
    [Arguments("1.0~rc1~git123", "1.0~rc1", -1)]
    [Arguments("1.0~rc1", "1.0~rc1~git123", 1)]
    public async Task Compare_TildeSorting(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Caret sorting
    [Test]
    [Arguments("1.0^", "1.0^", 0)]
    [Arguments("1.0^", "1.0", 1)]
    [Arguments("1.0", "1.0^", -1)]
    [Arguments("1.0^git1", "1.0^git1", 0)]
    [Arguments("1.0^git1", "1.0", 1)]
    [Arguments("1.0", "1.0^git1", -1)]
    [Arguments("1.0^git1", "1.0^git2", -1)]
    [Arguments("1.0^git2", "1.0^git1", 1)]
    [Arguments("1.0^git1", "1.01", -1)]
    [Arguments("1.01", "1.0^git1", 1)]
    [Arguments("1.0^20160101", "1.0^20160101", 0)]
    [Arguments("1.0^20160101", "1.0.1", -1)]
    [Arguments("1.0.1", "1.0^20160101", 1)]
    [Arguments("1.0^20160101^git1", "1.0^20160101^git1", 0)]
    [Arguments("1.0^20160102", "1.0^20160101^git1", 1)]
    [Arguments("1.0^20160101^git1", "1.0^20160102", -1)]
    public async Task Compare_CaretSorting(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Tilde and caret combined
    [Test]
    [Arguments("1.0~rc1^git1", "1.0~rc1^git1", 0)]
    [Arguments("1.0~rc1^git1", "1.0~rc1", 1)]
    [Arguments("1.0~rc1", "1.0~rc1^git1", -1)]
    [Arguments("1.0^git1~pre", "1.0^git1~pre", 0)]
    [Arguments("1.0^git1", "1.0^git1~pre", 1)]
    [Arguments("1.0^git1~pre", "1.0^git1", -1)]
    public async Task Compare_TildeAndCaret(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Epoch handling
    [Test]
    [Arguments("1:1.0", "1.0", 1)]
    [Arguments("1.0", "1:1.0", -1)]
    [Arguments("1:1.0", "1:1.0", 0)]
    [Arguments("2:1.0", "1:2.0", 1)]
    [Arguments("0:1.0", "1.0", 0)]
    public async Task Compare_Epoch(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}
