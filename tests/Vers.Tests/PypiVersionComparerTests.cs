using Vers.Schemes;

namespace Vers.Tests;

/// <summary>
/// Test cases for PyPI (PEP 440) version comparison.
/// </summary>
public class PypiVersionComparerTests
{
    private readonly PypiVersionComparer _comparer = PypiVersionComparer.Instance;

    // Basic release comparison
    [Test]
    [Arguments("1.0", "1.0", 0)]
    [Arguments("1.0", "2.0", -1)]
    [Arguments("1.0.0", "1.0", 0)]
    [Arguments("1.0.0.0", "1.0", 0)]
    [Arguments("1.0.1", "1.0.2", -1)]
    [Arguments("1.10", "1.9", 1)]
    // Pre-release
    [Arguments("1.0a1", "1.0", -1)]
    [Arguments("1.0b1", "1.0", -1)]
    [Arguments("1.0rc1", "1.0", -1)]
    [Arguments("1.0a1", "1.0b1", -1)]
    [Arguments("1.0b1", "1.0rc1", -1)]
    [Arguments("1.0a1", "1.0a2", -1)]
    [Arguments("1.0alpha1", "1.0a1", 0)]
    [Arguments("1.0beta1", "1.0b1", 0)]
    [Arguments("1.0c1", "1.0rc1", 0)]
    [Arguments("1.0preview1", "1.0rc1", 0)]
    // Post-release
    [Arguments("1.0.post1", "1.0", 1)]
    [Arguments("1.0.post1", "1.0.post2", -1)]
    [Arguments("1.0.post1", "1.1", -1)]
    // Dev-release
    [Arguments("1.0.dev1", "1.0", -1)]
    [Arguments("1.0.dev1", "1.0.dev2", -1)]
    [Arguments("1.0a1.dev1", "1.0a1", -1)]
    [Arguments("1.0.post1.dev1", "1.0.post1", -1)]
    // Epoch
    [Arguments("1!1.0", "2.0", 1)]
    [Arguments("2.0", "1!1.0", -1)]
    [Arguments("1!1.0", "1!2.0", -1)]
    // Mixed
    [Arguments("1.0a1", "1.0.post1", -1)]
    [Arguments("1.0.dev1", "1.0a1", -1)]
    public async Task Compare_Pep440(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Local version
    [Test]
    [Arguments("1.0+local1", "1.0", 1)]
    [Arguments("1.0+local1", "1.0+local2", -1)]
    public async Task Compare_LocalVersion(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }

    // Separator normalization
    [Test]
    [Arguments("1.0-post1", "1.0.post1", 0)]
    [Arguments("1.0_post1", "1.0.post1", 0)]
    public async Task Compare_SeparatorNormalization(string v1, string v2, int expectedSign)
    {
        var result = _comparer.Compare(v1, v2);
        await Assert.That(result == 0 ? 0 : (result > 0 ? 1 : -1)).IsEqualTo(expectedSign);
    }
}
