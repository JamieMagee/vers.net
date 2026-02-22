namespace Vers;

/// <summary>
/// Abstracts version comparison for a specific versioning scheme.
/// </summary>
public interface IVersionComparer
{
    /// <summary>
    /// Compares two version strings. Returns negative if v1 &lt; v2,
    /// zero if equal, positive if v1 &gt; v2.
    /// </summary>
    int Compare(string version1, string version2);

    /// <summary>
    /// Returns a normalized form of the version string.
    /// Default implementation returns the input unchanged.
    /// </summary>
    string Normalize(string version);

    /// <summary>
    /// Returns true if the version string is valid for this scheme.
    /// </summary>
    bool IsValid(string version);
}
