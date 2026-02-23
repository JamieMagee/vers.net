using Vers.Schemes;

namespace Vers.Tests;

/// <summary>
/// Test cases derived from dpkg's version comparison behavior
/// per Debian Policy §5.6.12.
/// </summary>
public class DebianVersionComparerTests
{
    private readonly DebianVersionComparer _comparer = DebianVersionComparer.Instance;

    // Basic numeric comparison
    [Test]
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("2.0", "1.0", 1)]
    [Arguments("1.0.1", "1.0.1", 0)]
    [Arguments("1.0.1", "1.0.2", -1)]
    // Epoch comparison
    [Arguments("1:1.0", "1.0", 1)]
    [Arguments("1.0", "1:1.0", -1)]
    [Arguments("1:1.0", "1:1.0", 0)]
    [Arguments("2:1.0", "1:2.0", 1)]
    [Arguments("0:1.0", "1.0", 0)]
    // Debian revision
    [Arguments("1.0-1", "1.0-1", 0)]
    [Arguments("1.0-1", "1.0-2", -1)]
    [Arguments("1.0-2", "1.0-1", 1)]
    [Arguments("1.0", "1.0-0", 0)]
    // Tilde sorts before everything
    [Arguments("1.0~rc1", "1.0", -1)]
    [Arguments("1.0", "1.0~rc1", 1)]
    [Arguments("1.0~rc1", "1.0~rc2", -1)]
    [Arguments("1.0~beta1", "1.0~rc1", -1)]
    [Arguments("1.0~alpha", "1.0~beta", -1)]
    [Arguments("1.0~~", "1.0~", -1)]
    [Arguments("1.0~", "1.0", -1)]
    // Letters sort before non-letters
    [Arguments("1.0a", "1.0a", 0)]
    [Arguments("1.0a", "1.0b", -1)]
    // Numeric segments compared numerically
    [Arguments("1.10", "1.9", 1)]
    [Arguments("1.01", "1.1", 0)]
    // Complex versions
    [Arguments("2:1.0-1", "1:2.0-1", 1)]
    [Arguments("1.0+dfsg1", "1.0", 1)]
    [Arguments("1.0+dfsg1-1", "1.0-1", 1)]
    public async Task Compare_DebianVersions(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Tilde deep nesting (from Debian Policy footnote 7)
    [Test]
    [Arguments("1.0~beta1~svn1245", "1.0~beta1", -1)]
    [Arguments("1.0~beta1", "1.0", -1)]
    public async Task Compare_TildePrerelease(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Sort order per spec: ~~, ~~a, ~, empty, a
    [Test]
    public async Task Compare_TildeSortOrder()
    {
        // ~~ < ~~a
        await Assert.That(_comparer.Compare("1.0~~", "1.0~~a") < 0).IsTrue();
        // ~~a < ~
        await Assert.That(_comparer.Compare("1.0~~a", "1.0~") < 0).IsTrue();
        // ~ < (empty = 1.0)
        await Assert.That(_comparer.Compare("1.0~", "1.0") < 0).IsTrue();
        // (empty) < a
        await Assert.That(_comparer.Compare("1.0", "1.0a") < 0).IsTrue();
    }
}
