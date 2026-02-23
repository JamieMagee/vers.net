using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements ALPM (Arch Linux Package Manager) version comparison.
///
/// Format: [epoch:]version[-release]
///
/// Uses the same segment comparison algorithm as rpmvercmp:
/// - Segments are contiguous runs of digits or letters
/// - Digit segments compared numerically, alpha segments lexicographically
/// - When types differ, numeric segments sort after alpha segments
/// - Non-alphanumeric characters are separators
///
/// Special rules:
/// - Epoch 0 is equivalent to no epoch
/// - Release defaults to 1 (so "1.5" equals "1.5-1")
/// </summary>
public sealed class AlpmVersionComparer : IVersionComparer
{
    public static readonly AlpmVersionComparer Instance = new AlpmVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        SplitComponents(version1, out long epoch1, out string ver1, out long rel1);
        SplitComponents(version2, out long epoch2, out string ver2, out long rel2);

        // 1. Compare epoch
        int cmp = epoch1.CompareTo(epoch2);
        if (cmp != 0)
        {
            return cmp;
        }

        // 2. Compare version using rpmvercmp-style algorithm
        cmp = Vercmp(ver1, ver2);
        if (cmp != 0)
        {
            return cmp;
        }

        // 3. Compare release
        return rel1.CompareTo(rel2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static void SplitComponents(
        string version,
        out long epoch,
        out string ver,
        out long release
    )
    {
        epoch = 0;
        release = 1; // default release is 1
        ver = version;

        // Extract epoch (N:...)
        int colonIdx = ver.IndexOf(':');
        if (colonIdx > 0)
        {
            var epochStr = ver.Substring(0, colonIdx);
            if (long.TryParse(epochStr, NumberStyles.None, CultureInfo.InvariantCulture, out epoch))
            {
                ver = ver.Substring(colonIdx + 1);
            }
            else
            {
                epoch = 0;
            }
        }

        // Extract release (...-N) — last hyphen
        int dashIdx = ver.LastIndexOf('-');
        if (dashIdx > 0)
        {
            var relStr = ver.Substring(dashIdx + 1);
            if (long.TryParse(relStr, NumberStyles.None, CultureInfo.InvariantCulture, out release))
            {
                ver = ver.Substring(0, dashIdx);
            }
            else
            {
                release = 1;
            }
        }
    }

    /// <summary>
    /// rpmvercmp-style segment comparison (without tilde/caret which ALPM doesn't use).
    /// </summary>
    private static int Vercmp(string a, string b)
    {
        if (a == b)
        {
            return 0;
        }

        int i1 = 0,
            i2 = 0;
        int len1 = a.Length,
            len2 = b.Length;

        while (i1 < len1 || i2 < len2)
        {
            // Skip non-alphanumeric separators
            while (i1 < len1 && !char.IsLetterOrDigit(a[i1]))
            {
                i1++;
            }

            while (i2 < len2 && !char.IsLetterOrDigit(b[i2]))
            {
                i2++;
            }

            if (i1 >= len1 || i2 >= len2)
            {
                break;
            }

            int start1 = i1,
                start2 = i2;
            bool isNum;

            if (char.IsDigit(a[i1]))
            {
                while (i1 < len1 && char.IsDigit(a[i1]))
                {
                    i1++;
                }

                while (i2 < len2 && char.IsDigit(b[i2]))
                {
                    i2++;
                }

                isNum = true;
            }
            else
            {
                while (i1 < len1 && char.IsLetter(a[i1]))
                {
                    i1++;
                }

                while (i2 < len2 && char.IsLetter(b[i2]))
                {
                    i2++;
                }

                isNum = false;
            }

            var seg1 = a.Substring(start1, i1 - start1);
            var seg2 = b.Substring(start2, i2 - start2);

            if (seg2.Length == 0)
            {
                return isNum ? 1 : -1;
            }

            if (isNum)
            {
                var s1 = seg1.TrimStart('0');
                var s2 = seg2.TrimStart('0');

                if (s1.Length != s2.Length)
                {
                    return s1.Length > s2.Length ? 1 : -1;
                }

                int cmp = string.Compare(s1, s2, StringComparison.Ordinal);
                if (cmp != 0)
                {
                    return cmp < 0 ? -1 : 1;
                }
            }
            else
            {
                int cmp = string.Compare(seg1, seg2, StringComparison.Ordinal);
                if (cmp != 0)
                {
                    return cmp < 0 ? -1 : 1;
                }
            }
        }

        if (i1 >= len1 && i2 >= len2)
        {
            return 0;
        }

        if (i1 >= len1)
        {
            return -1;
        }

        return 1;
    }
}
