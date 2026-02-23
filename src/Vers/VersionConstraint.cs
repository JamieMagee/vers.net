using System;

namespace Vers;

/// <summary>
/// An immutable version constraint consisting of a comparator and a version string.
/// </summary>
public sealed class VersionConstraint : IEquatable<VersionConstraint>
{
    public Comparator Comparator { get; }
    public string Version { get; }

    public VersionConstraint(Comparator comparator, string version)
    {
        Comparator = comparator;
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <summary>
    /// Parses a single version constraint string such as "&gt;=1.2.3" or "1.2.3".
    /// </summary>
    public static VersionConstraint Parse(string constraintString)
    {
        if (string.IsNullOrWhiteSpace(constraintString))
        {
            throw new VersException("Version constraint string is empty.");
        }

        var s = constraintString.Trim();

        if (s == "*")
        {
            return new VersionConstraint(Comparator.Wildcard, "");
        }

        if (ComparatorExtensions.TryParsePrefix(s, 0, out var comparator, out var length))
        {
            // Wildcard must be alone with no version
            if (comparator == Comparator.Wildcard)
            {
                throw new VersException(
                    $"Version constraint '{constraintString}': wildcard '*' must appear alone."
                );
            }

            var version = s.Substring(length).Trim();
            if (string.IsNullOrEmpty(version))
            {
                throw new VersException(
                    $"Version constraint '{constraintString}' has a comparator but no version."
                );
            }

            version = UrlDecode(version);
            return new VersionConstraint(comparator, version);
        }

        // No comparator prefix means equality.
        // Handle explicit '=' prefix: strip it so "=1.2.3" becomes Equal("1.2.3").
        var raw = s;
        if (raw.Length > 0 && raw[0] == '=')
        {
            raw = raw.Substring(1).Trim();
            if (string.IsNullOrEmpty(raw))
            {
                throw new VersException(
                    $"Version constraint '{constraintString}' has '=' but no version."
                );
            }
        }

        return new VersionConstraint(Comparator.Equal, UrlDecode(raw));
    }

    /// <summary>
    /// Tests whether a given version satisfies this constraint using the provided comparer.
    /// </summary>
    public bool Matches(string testedVersion, IVersionComparer comparer)
    {
        if (Comparator == Comparator.Wildcard)
        {
            return true;
        }

        int cmp = comparer.Compare(testedVersion, Version);
        switch (Comparator)
        {
            case Comparator.Equal:
                return cmp == 0;
            case Comparator.NotEqual:
                return cmp != 0;
            case Comparator.LessThan:
                return cmp < 0;
            case Comparator.LessThanOrEqual:
                return cmp <= 0;
            case Comparator.GreaterThan:
                return cmp > 0;
            case Comparator.GreaterThanOrEqual:
                return cmp >= 0;
            default:
                return false;
        }
    }

    public override string ToString()
    {
        if (Comparator == Comparator.Wildcard)
        {
            return "*";
        }

        var prefix = Comparator == Comparator.Equal ? "" : Comparator.ToSymbol();
        return prefix + UrlEncode(Version);
    }

    public bool Equals(VersionConstraint? other)
    {
        if (other is null)
        {
            return false;
        }

        return Comparator == other.Comparator && Version == other.Version;
    }

    public override bool Equals(object? obj) => Equals(obj as VersionConstraint);

    public override int GetHashCode() => Comparator.GetHashCode() ^ Version.GetHashCode();

    private static string UrlDecode(string value)
    {
        if (value.IndexOf('%') < 0)
        {
            return value;
        }

        return Uri.UnescapeDataString(value);
    }

    private static string UrlEncode(string value)
    {
        // Only encode comparator/separator characters: > < = ! * | %
        if (value.IndexOfAny(new[] { '>', '<', '=', '!', '*', '|', '%' }) < 0)
        {
            return value;
        }

        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '>':
                    sb.Append("%3E");
                    break;
                case '<':
                    sb.Append("%3C");
                    break;
                case '=':
                    sb.Append("%3D");
                    break;
                case '!':
                    sb.Append("%21");
                    break;
                case '*':
                    sb.Append("%2A");
                    break;
                case '|':
                    sb.Append("%7C");
                    break;
                case '%':
                    sb.Append("%25");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
