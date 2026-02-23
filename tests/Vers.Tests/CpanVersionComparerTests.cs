using Vers.Schemes;

namespace Vers.Tests;

/// <summary>
/// Test cases for CPAN (Perl) version comparison.
/// </summary>
public class CpanVersionComparerTests
{
    private readonly CpanVersionComparer _comparer = CpanVersionComparer.Instance;

    // Dotted-decimal comparison
    [Test]
    [Arguments("v1.2.3", "v1.2.3", 0)]
    [Arguments("v1.2.3", "v1.2.4", -1)]
    [Arguments("v1.2.3", "v1.3.0", -1)]
    [Arguments("v1.2.3", "v2.0.0", -1)]
    [Arguments("v1.2.0", "v1.2", 0)]
    // Decimal comparison
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("1.02", "1.02", 0)]
    // Decimal-to-dotted equivalence: 1.02 = v1.20.0
    [Arguments("1.002003", "v1.2.3", 0)]
    [Arguments("1.02", "v1.20.0", 0)]
    // 3-digit grouping
    [Arguments("1.0203", "1.0201", 1)]
    [Arguments("1.0203", "v1.20.300", 0)]
    // Leading v
    [Arguments("v1.2.3", "1.2.3", 0)]
    // Alpha (underscore) sorts before equivalent non-alpha
    [Arguments("1.02_03", "1.0203", -1)]
    [Arguments("v1.2.3_4", "v1.2.34", -1)]
    // Decimal: 1.10 = v1.100.0, 1.9 = v1.900.0, so 1.9 > 1.10
    [Arguments("1.10", "1.9", -1)]
    public async Task Compare_CpanVersions(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}
