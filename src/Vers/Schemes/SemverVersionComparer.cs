using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements SemVer 2.0.0 version comparison.
/// Format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
/// Build metadata is ignored for comparison.
/// Pre-release versions have lower precedence than the associated normal version.
/// </summary>
public sealed class SemverVersionComparer : IVersionComparer
{
    public static readonly SemverVersionComparer Instance = new SemverVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
            return 0;

        var v1 = ParseSemver(version1);
        var v2 = ParseSemver(version2);

        // Compare major.minor.patch
        int cmp = v1.Major.CompareTo(v2.Major);
        if (cmp != 0)
            return cmp;

        cmp = v1.Minor.CompareTo(v2.Minor);
        if (cmp != 0)
            return cmp;

        cmp = v1.Patch.CompareTo(v2.Patch);
        if (cmp != 0)
            return cmp;

        // Pre-release comparison
        // No pre-release > has pre-release
        if (v1.Prerelease == null && v2.Prerelease == null)
            return 0;
        if (v1.Prerelease == null)
            return 1;
        if (v2.Prerelease == null)
            return -1;

        return ComparePrerelease(v1.Prerelease, v2.Prerelease);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version)
    {
        try
        {
            ParseSemver(version);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ComparePrerelease(string pre1, string pre2)
    {
        var ids1 = pre1.Split('.');
        var ids2 = pre2.Split('.');

        int len = Math.Min(ids1.Length, ids2.Length);
        for (int i = 0; i < len; i++)
        {
            var a = ids1[i];
            var b = ids2[i];

            bool aNum = long.TryParse(
                a,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long aVal
            );
            bool bNum = long.TryParse(
                b,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long bVal
            );

            if (aNum && bNum)
            {
                int cmp = aVal.CompareTo(bVal);
                if (cmp != 0)
                    return cmp;
            }
            else if (aNum)
            {
                // Numeric identifiers always have lower precedence
                return -1;
            }
            else if (bNum)
            {
                return 1;
            }
            else
            {
                int cmp = string.Compare(a, b, StringComparison.Ordinal);
                if (cmp != 0)
                    return cmp;
            }
        }

        return ids1.Length.CompareTo(ids2.Length);
    }

    private struct SemverParts
    {
        public long Major;
        public long Minor;
        public long Patch;
        public string? Prerelease;
    }

    private static SemverParts ParseSemver(string version)
    {
        if (string.IsNullOrEmpty(version))
            throw new VersException("Version string is empty.");

        var s = version;

        // Strip optional leading 'v' or 'V'
        if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V'))
            s = s.Substring(1);

        // Remove build metadata (+...)
        var plusIdx = s.IndexOf('+');
        if (plusIdx >= 0)
            s = s.Substring(0, plusIdx);

        // Extract pre-release (-...)
        string? prerelease = null;
        var dashIdx = s.IndexOf('-');
        if (dashIdx >= 0)
        {
            prerelease = s.Substring(dashIdx + 1);
            s = s.Substring(0, dashIdx);
        }

        var parts = s.Split('.');
        // Be lenient: support M, M.m, and M.m.p
        long major = 0,
            minor = 0,
            patch = 0;

        if (
            parts.Length >= 1
            && !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major)
        )
            throw new VersException($"Invalid semver major version in '{version}'.");
        if (
            parts.Length >= 2
            && !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor)
        )
            throw new VersException($"Invalid semver minor version in '{version}'.");
        if (
            parts.Length >= 3
            && !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out patch)
        )
            throw new VersException($"Invalid semver patch version in '{version}'.");

        return new SemverParts
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            Prerelease = prerelease,
        };
    }
}
