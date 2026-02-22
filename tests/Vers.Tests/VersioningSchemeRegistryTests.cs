using Xunit;

namespace Vers.Tests;

public class VersioningSchemeRegistryTests
{
    [Theory]
    [InlineData("npm")]
    [InlineData("semver")]
    [InlineData("golang")]
    [InlineData("cargo")]
    [InlineData("generic")]
    [InlineData("intdot")]
    [InlineData("lexicographic")]
    [InlineData("datetime")]
    public void BuiltInSchemes_AreRegistered(string scheme)
    {
        Assert.True(VersioningSchemeRegistry.IsKnown(scheme));
        Assert.NotNull(VersioningSchemeRegistry.Get(scheme));
    }

    [Fact]
    public void UnknownScheme_FallsBackToGeneric()
    {
        var comparer = VersioningSchemeRegistry.GetComparer("unknown-scheme");
        Assert.IsType<Schemes.GenericVersionComparer>(comparer);
    }

    [Fact]
    public void Register_CustomScheme()
    {
        var custom = new VersioningScheme("myscheme", Schemes.GenericVersionComparer.Instance);
        VersioningSchemeRegistry.Register(custom);
        Assert.True(VersioningSchemeRegistry.IsKnown("myscheme"));
    }
}
