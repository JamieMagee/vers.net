using System;
using System.Collections.Generic;
using Vers.Schemes;

namespace Vers;

/// <summary>
/// Thread-safe registry of known versioning schemes.
/// Falls back to <see cref="GenericVersionComparer"/> for unknown schemes.
/// </summary>
public static class VersioningSchemeRegistry
{
    private static readonly object Lock = new();
    private static readonly Dictionary<string, IVersioningScheme> Schemes = new Dictionary<
        string,
        IVersioningScheme
    >(StringComparer.OrdinalIgnoreCase);

    static VersioningSchemeRegistry()
    {
        // Register built-in schemes
        var semver = new SemverVersionComparer();
        var generic = new GenericVersionComparer();
        var intdot = new IntdotVersionComparer();
        var lexicographic = new LexicographicVersionComparer();
        var datetime = new DatetimeVersionComparer();
        var maven = new MavenVersionComparer();
        var nuget = new NuGetVersionComparer();
        var gentoo = new GentooVersionComparer();
        var rpm = new RpmVersionComparer();
        var openssl = new OpensslVersionComparer();
        var alpm = new AlpmVersionComparer();
        var debian = new DebianVersionComparer();
        var conan = new ConanVersionComparer();
        var pypi = new PypiVersionComparer();
        var gem = new GemVersionComparer();

        RegisterBuiltIn("semver", semver);
        RegisterBuiltIn("npm", semver);
        RegisterBuiltIn("golang", semver);
        RegisterBuiltIn("cargo", semver);
        RegisterBuiltIn("composer", semver);

        RegisterBuiltIn("generic", generic);

        RegisterBuiltIn("intdot", intdot);
        RegisterBuiltIn("lexicographic", lexicographic);
        RegisterBuiltIn("datetime", datetime);

        RegisterBuiltIn("maven", maven);
        RegisterBuiltIn("nuget", nuget);

        RegisterBuiltIn("gentoo", gentoo);
        RegisterBuiltIn("ebuild", gentoo);
        RegisterBuiltIn("alpine", gentoo);
        RegisterBuiltIn("apk", gentoo);

        RegisterBuiltIn("rpm", rpm);

        RegisterBuiltIn("openssl", openssl);

        RegisterBuiltIn("alpm", alpm);
        RegisterBuiltIn("arch", alpm);

        RegisterBuiltIn("deb", debian);

        RegisterBuiltIn("conan", conan);

        RegisterBuiltIn("pypi", pypi);

        RegisterBuiltIn("gem", gem);

        RegisterBuiltIn("none", generic);
        RegisterBuiltIn("all", generic);
    }

    private static void RegisterBuiltIn(string scheme, IVersionComparer comparer)
    {
        Schemes[scheme] = new VersioningScheme(scheme, comparer);
    }

    /// <summary>
    /// Registers a custom versioning scheme. Overwrites any existing scheme with the same name.
    /// </summary>
    public static void Register(IVersioningScheme scheme)
    {
        if (scheme == null)
        {
            throw new ArgumentNullException(nameof(scheme));
        }

        lock (Lock)
        {
            Schemes[scheme.Scheme] = scheme;
        }
    }

    /// <summary>
    /// Gets the registered scheme, or null if not found.
    /// </summary>
    public static IVersioningScheme? Get(string scheme)
    {
        lock (Lock)
        {
            return Schemes.TryGetValue(scheme, out var s) ? s : null;
        }
    }

    /// <summary>
    /// Returns true if the scheme is registered.
    /// </summary>
    public static bool IsKnown(string scheme)
    {
        lock (Lock)
        {
            return Schemes.ContainsKey(scheme);
        }
    }

    /// <summary>
    /// Gets the <see cref="IVersionComparer"/> for the given scheme,
    /// falling back to <see cref="GenericVersionComparer"/> if not registered.
    /// </summary>
    public static IVersionComparer GetComparer(string scheme)
    {
        var s = Get(scheme);
        return s?.Comparer ?? GenericVersionComparer.Instance;
    }
}
