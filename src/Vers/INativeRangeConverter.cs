namespace Vers;

/// <summary>
/// Converts between a native ecosystem range notation and vers format.
/// </summary>
public interface INativeRangeConverter
{
    /// <summary>
    /// Converts a native range string (e.g., npm's "^1.2.3" or NuGet's "[1.0, 2.0)")
    /// to a <see cref="VersRange"/>.
    /// </summary>
    VersRange FromNative(string nativeRange);

    /// <summary>
    /// Converts a <see cref="VersRange"/> back to the native range notation.
    /// May throw if the range cannot be represented in the native format.
    /// </summary>
    string ToNative(VersRange range);
}
