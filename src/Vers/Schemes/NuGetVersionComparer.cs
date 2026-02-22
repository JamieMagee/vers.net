using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements NuGet version comparison.
/// NuGet versions follow SemVer 2.0.0 but with case-insensitive comparison
/// for pre-release labels. Build metadata is ignored.
/// Supports legacy 4-part versions (MAJOR.MINOR.PATCH.REVISION).
/// </summary>
public sealed class NuGetVersionComparer : IVersionComparer
{
    public static readonly NuGetVersionComparer Instance = new NuGetVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (string.Equals(version1, version2, StringComparison.OrdinalIgnoreCase))
            return 0;

        var v1 = ParseNuGet(version1);
        var v2 = ParseNuGet(version2);

        int cmp = v1.Major.CompareTo(v2.Major);
        if (cmp != 0)
            return cmp;

        cmp = v1.Minor.CompareTo(v2.Minor);
        if (cmp != 0)
            return cmp;

        cmp = v1.Patch.CompareTo(v2.Patch);
        if (cmp != 0)
            return cmp;

        cmp = v1.Revision.CompareTo(v2.Revision);
        if (cmp != 0)
            return cmp;

        // Pre-release comparison (case-insensitive)
        if (v1.Prerelease == null && v2.Prerelease == null)
            return 0;
        if (v1.Prerelease == null)
            return 1; // release > pre-release
        if (v2.Prerelease == null)
            return -1;

        return ComparePrerelease(v1.Prerelease, v2.Prerelease);
    }

    public string Normalize(string version)
    {
        // NuGet: lowercase everything except build metadata (after +)
        if (string.IsNullOrEmpty(version))
            return version;

        var plusIdx = version.IndexOf('+');
        if (plusIdx < 0)
            return version.ToLowerInvariant();

        return version.Substring(0, plusIdx).ToLowerInvariant() + version.Substring(plusIdx);
    }

    public bool IsValid(string version)
    {
        try
        {
            ParseNuGet(version);
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
                return -1;
            }
            else if (bNum)
            {
                return 1;
            }
            else
            {
                // Case-insensitive string comparison for NuGet pre-release labels
                int cmp = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0)
                    return cmp;
            }
        }

        return ids1.Length.CompareTo(ids2.Length);
    }

    private struct NuGetParts
    {
        public long Major;
        public long Minor;
        public long Patch;
        public long Revision;
        public string? Prerelease;
    }

    private static NuGetParts ParseNuGet(string version)
    {
        if (string.IsNullOrEmpty(version))
            throw new VersException("NuGet version string is empty.");

        var s = version;

        // Remove build metadata
        var plusIdx = s.IndexOf('+');
        if (plusIdx >= 0)
            s = s.Substring(0, plusIdx);

        // Extract pre-release
        string? prerelease = null;
        var dashIdx = s.IndexOf('-');
        if (dashIdx >= 0)
        {
            prerelease = s.Substring(dashIdx + 1);
            s = s.Substring(0, dashIdx);
        }

        var parts = s.Split('.');
        long major = 0,
            minor = 0,
            patch = 0,
            revision = 0;

        if (parts.Length >= 1)
            long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major);
        if (parts.Length >= 2)
            long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor);
        if (parts.Length >= 3)
            long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out patch);
        if (parts.Length >= 4)
            long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out revision);

        return new NuGetParts
        {
            Major = major,
            Minor = minor,
            Patch = patch,
            Revision = revision,
            Prerelease = prerelease,
        };
    }
}
