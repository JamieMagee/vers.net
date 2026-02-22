namespace Vers.Tests;

public class VersioningSchemeRegistryTests
{
    [Test]
    [Arguments("npm")]
    [Arguments("semver")]
    [Arguments("golang")]
    [Arguments("cargo")]
    [Arguments("generic")]
    [Arguments("intdot")]
    [Arguments("lexicographic")]
    [Arguments("datetime")]
    public async Task BuiltInSchemes_AreRegistered(string scheme)
    {
        await Assert.That(VersioningSchemeRegistry.IsKnown(scheme)).IsTrue();
        await Assert.That(VersioningSchemeRegistry.Get(scheme)).IsNotNull();
    }

    [Test]
    public async Task UnknownScheme_FallsBackToGeneric()
    {
        var comparer = VersioningSchemeRegistry.GetComparer("unknown-scheme");
        await Assert.That(comparer).IsTypeOf<Schemes.GenericVersionComparer>();
    }

    [Test]
    public async Task Register_CustomScheme()
    {
        var custom = new VersioningScheme("myscheme", Schemes.GenericVersionComparer.Instance);
        VersioningSchemeRegistry.Register(custom);
        await Assert.That(VersioningSchemeRegistry.IsKnown("myscheme")).IsTrue();
    }
}
