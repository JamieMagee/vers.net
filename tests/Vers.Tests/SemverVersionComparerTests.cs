using Vers.Schemes;

namespace Vers.Tests;

public class SemverVersionComparerTests
{
    private readonly SemverVersionComparer _comparer = SemverVersionComparer.Instance;

    [Test]
    [Arguments("1.0.0", "1.0.0", 0)]
    [Arguments("1.0.0", "2.0.0", -1)]
    [Arguments("2.0.0", "1.0.0", 1)]
    [Arguments("1.0.0", "1.1.0", -1)]
    [Arguments("1.0.0", "1.0.1", -1)]
    [Arguments("1.0.0-alpha", "1.0.0-beta", -1)]
    [Arguments("1.0.0-alpha", "1.0.0", -1)]
    [Arguments("1.0.0", "1.0.0-alpha", 1)]
    [Arguments("1.0.0-alpha.1", "1.0.0-alpha.2", -1)]
    [Arguments("1.0.0-1", "1.0.0-2", -1)]
    [Arguments("1.0.0-alpha", "1.0.0-alpha.1", -1)]
    [Arguments("1.0.0+build1", "1.0.0+build2", 0)]
    public async Task Compare_FollowsSemver(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    [Test]
    [Arguments("v1.0.0", "1.0.0", 0)]
    public async Task Compare_ToleratesLeadingV(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    [Test]
    [Arguments("1.0.0", true)]
    [Arguments("1.0.0-alpha", true)]
    [Arguments("1.0.0+build", true)]
    [Arguments("not-a-version", false)]
    public async Task IsValid(string version, bool expected)
    {
        await Assert.That(_comparer.IsValid(version)).IsEqualTo(expected);
    }
}
