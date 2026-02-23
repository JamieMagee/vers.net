namespace Vers;

/// <summary>
/// Represents a versioning scheme with its name and version comparer.
/// </summary>
public interface IVersioningScheme
{
    string Scheme { get; }
    IVersionComparer Comparer { get; }
}

/// <summary>
/// Simple implementation of IVersioningScheme.
/// </summary>
public sealed class VersioningScheme(string scheme, IVersionComparer comparer) : IVersioningScheme
{
    public string Scheme { get; } = scheme;
    public IVersionComparer Comparer { get; } = comparer;
}
