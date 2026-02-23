using Vers.Schemes;

namespace Vers.Tests;

/// <summary>
/// Test cases for RubyGems version comparison.
/// </summary>
public class GemVersionComparerTests
{
    private readonly GemVersionComparer _comparer = GemVersionComparer.Instance;

    // Basic numeric comparison
    [Test]
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("2.0", "1.0", 1)]
    [Arguments("1.0.0", "1.0", 0)]
    [Arguments("1.0.1", "1.0.2", -1)]
    [Arguments("1.10", "1.9", 1)]
    // Pre-release (string segments sort before numeric)
    [Arguments("1.0.beta", "1.0", -1)]
    [Arguments("1.0.alpha", "1.0.beta", -1)]
    [Arguments("1.0.beta", "1.0.rc", -1)]
    [Arguments("1.0.alpha.1", "1.0.alpha.2", -1)]
    [Arguments("1.0.0.beta1", "1.0.0", -1)]
    // Multi-segment
    [Arguments("1.2.3", "1.2.3", 0)]
    [Arguments("1.2.3.4", "1.2.3.4", 0)]
    [Arguments("1.2.3", "1.2.4", -1)]
    // Trailing zeros
    [Arguments("1.0.0.0", "1", 0)]
    [Arguments("1.0.0", "1.0", 0)]
    public async Task Compare_GemVersions(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}
