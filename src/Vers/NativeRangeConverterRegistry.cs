using System;
using System.Collections.Generic;
using Vers.NativeRanges;

namespace Vers;

/// <summary>
/// Registry of native range converters for each versioning scheme.
/// </summary>
public static class NativeRangeConverterRegistry
{
    private static readonly object Lock = new object();
    private static readonly Dictionary<string, INativeRangeConverter> Converters = new(
        StringComparer.OrdinalIgnoreCase
    );

    static NativeRangeConverterRegistry()
    {
        RegisterBuiltIn("npm", NpmNativeRangeConverter.Instance);
        RegisterBuiltIn("pypi", PypiNativeRangeConverter.Instance);
        RegisterBuiltIn("nuget", NugetNativeRangeConverter.Instance);
        RegisterBuiltIn("gem", GemNativeRangeConverter.Instance);
        RegisterBuiltIn("conan", ConanNativeRangeConverter.Instance);
        RegisterBuiltIn("openssl", OpensslNativeRangeConverter.Instance);
        RegisterBuiltIn("nginx", NginxNativeRangeConverter.Instance);
    }

    private static void RegisterBuiltIn(string scheme, INativeRangeConverter converter)
    {
        Converters[scheme] = converter;
    }

    /// <summary>
    /// Registers a custom native range converter.
    /// </summary>
    public static void Register(string scheme, INativeRangeConverter converter)
    {
        if (scheme == null)
        {
            throw new ArgumentNullException(nameof(scheme));
        }

        if (converter == null)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        lock (Lock)
        {
            Converters[scheme] = converter;
        }
    }

    /// <summary>
    /// Gets the converter for the given scheme, or null if none is registered.
    /// </summary>
    public static INativeRangeConverter? Get(string scheme)
    {
        lock (Lock)
        {
            return Converters.TryGetValue(scheme, out var c) ? c : null;
        }
    }

    /// <summary>
    /// Converts a native range string to a <see cref="VersRange"/>.
    /// Throws if no converter is registered for the scheme.
    /// </summary>
    public static VersRange FromNative(string scheme, string nativeRange)
    {
        var converter =
            Get(scheme)
            ?? throw new VersException(
                $"No native range converter registered for scheme '{scheme}'."
            );
        return converter.FromNative(nativeRange);
    }
}
