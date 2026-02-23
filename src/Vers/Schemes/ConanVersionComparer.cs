using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements Conan (C/C++ package manager) version comparison.
///
/// Format: version[-prerelease][+build]
///
/// Version segments are dot-separated, compared as integers when numeric,
/// lexicographically otherwise. Missing trailing segments default to 0
/// (so 1 = 1.0 = 1.0.0).
///
/// Pre-release (-) sorts before the release version.
/// Build metadata (+) is compared (unlike SemVer where it's ignored).
/// Numeric segments sort before non-numeric segments.
/// </summary>
public sealed class ConanVersionComparer : IVersionComparer
{
    public static readonly ConanVersionComparer Instance = new ConanVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        SplitParts(version1, out string ver1, out string? pre1, out string? build1);
        SplitParts(version2, out string ver2, out string? pre2, out string? build2);

        // 1. Compare main version
        int cmp = CompareSegments(ver1, ver2);
        if (cmp != 0)
        {
            return cmp;
        }

        // 2. Compare pre-release: no pre-release > has pre-release
        if (pre1 == null && pre2 == null)
        {
            // fall through to build
        }
        else if (pre1 == null)
        {
            return 1;
        }
        else if (pre2 == null)
        {
            return -1;
        }
        else
        {
            cmp = CompareSegments(pre1, pre2);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        // 3. Compare build: no build < has build
        if (build1 == null && build2 == null)
        {
            return 0;
        }

        if (build1 == null)
        {
            return -1;
        }

        if (build2 == null)
        {
            return 1;
        }

        return CompareSegments(build1, build2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static void SplitParts(
        string version,
        out string ver,
        out string? prerelease,
        out string? build
    )
    {
        prerelease = null;
        build = null;
        ver = version;

        // Extract build (+)
        int plusIdx = ver.IndexOf('+');
        if (plusIdx >= 0)
        {
            build = ver.Substring(plusIdx + 1);
            ver = ver.Substring(0, plusIdx);
        }

        // Extract pre-release (-)
        int dashIdx = ver.IndexOf('-');
        if (dashIdx >= 0)
        {
            prerelease = ver.Substring(dashIdx + 1);
            ver = ver.Substring(0, dashIdx);
        }
    }

    /// <summary>
    /// Compares dot-separated segments. Numeric segments compared as integers,
    /// non-numeric compared lexicographically. Numeric sorts before non-numeric.
    /// Missing trailing segments default to 0.
    /// </summary>
    private static int CompareSegments(string a, string b)
    {
        var segs1 = a.Split('.');
        var segs2 = b.Split('.');

        int maxLen = Math.Max(segs1.Length, segs2.Length);
        for (int i = 0; i < maxLen; i++)
        {
            var s1 = i < segs1.Length ? segs1[i] : "";
            var s2 = i < segs2.Length ? segs2[i] : "";

            int cmp = CompareSegment(s1, s2);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    private static int CompareSegment(string s1, string s2)
    {
        // Empty segments treated as 0
        if (s1.Length == 0)
        {
            s1 = "0";
        }

        if (s2.Length == 0)
        {
            s2 = "0";
        }

        bool isNum1 = long.TryParse(
            s1,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long n1
        );
        bool isNum2 = long.TryParse(
            s2,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long n2
        );

        if (isNum1 && isNum2)
        {
            return n1.CompareTo(n2);
        }

        // Numeric sorts before non-numeric
        if (isNum1)
        {
            return -1;
        }

        if (isNum2)
        {
            return 1;
        }

        return string.Compare(s1, s2, StringComparison.Ordinal);
    }
}
