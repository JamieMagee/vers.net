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
public sealed class VersioningScheme : IVersioningScheme
{
    public string Scheme { get; }
    public IVersionComparer Comparer { get; }

    public VersioningScheme(string scheme, IVersionComparer comparer)
    {
        Scheme = scheme;
        Comparer = comparer;
    }
}
