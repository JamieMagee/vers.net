using System;

namespace Vers.Schemes;

/// <summary>
/// Implements RPM version comparison (rpmvercmp algorithm).
///
/// The algorithm processes version strings segment by segment:
/// 1. Skip non-alphanumeric separators (except ~ and ^)
/// 2. ~ sorts before everything (including empty string)
/// 3. ^ sorts after base version but before next release
/// 4. Segments are contiguous runs of digits or letters
/// 5. Digit segments compared numerically (leading zeros stripped)
/// 6. Alpha segments compared lexicographically
/// 7. When segment types differ, numeric beats alpha
///
/// RPM versions may include epoch (N:version-release) which is compared
/// as a numeric prefix before the version string.
/// </summary>
public sealed class RpmVersionComparer : IVersionComparer
{
    public static readonly RpmVersionComparer Instance = new();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        // Handle epoch:version format
        SplitEpoch(version1, out long epoch1, out string ver1);
        SplitEpoch(version2, out long epoch2, out string ver2);

        int cmp = epoch1.CompareTo(epoch2);
        if (cmp != 0)
        {
            return cmp;
        }

        return Rpmvercmp(ver1, ver2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static void SplitEpoch(string version, out long epoch, out string remainder)
    {
        epoch = 0;
        remainder = version;

        int colonIdx = version.IndexOf(':');
        if (colonIdx > 0)
        {
            var epochStr = version.Substring(0, colonIdx);
            if (
                long.TryParse(
                    epochStr,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out epoch
                )
            )
            {
                remainder = version.Substring(colonIdx + 1);
            }
            else
            {
                epoch = 0;
            }
        }
    }

    /// <summary>
    /// Port of the canonical rpmvercmp from rpm/rpmio/rpmvercmp.c
    /// </summary>
    private static int Rpmvercmp(string a, string b)
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
            // Skip non-alphanumeric, non-tilde, non-caret characters
            while (i1 < len1 && !char.IsLetterOrDigit(a[i1]) && a[i1] != '~' && a[i1] != '^')
            {
                i1++;
            }

            while (i2 < len2 && !char.IsLetterOrDigit(b[i2]) && b[i2] != '~' && b[i2] != '^')
            {
                i2++;
            }

            // Handle tilde: sorts before everything
            if ((i1 < len1 && a[i1] == '~') || (i2 < len2 && b[i2] == '~'))
            {
                if (i1 >= len1 || a[i1] != '~')
                {
                    return 1;
                }

                if (i2 >= len2 || b[i2] != '~')
                {
                    return -1;
                }

                i1++;
                i2++;
                continue;
            }

            // Handle caret: sorts after base version end, before next segment
            if ((i1 < len1 && a[i1] == '^') || (i2 < len2 && b[i2] == '^'))
            {
                if (i1 >= len1)
                {
                    return -1; // a ended, b has ^something → b is newer
                }

                if (i2 >= len2)
                {
                    return 1; // b ended, a has ^something → a is newer
                }

                if (a[i1] != '^')
                {
                    return 1; // a has normal segment, b has ^ → a is newer
                }

                if (b[i2] != '^')
                {
                    return -1;
                }

                i1++;
                i2++;
                continue;
            }

            // If either string ended, we're done with the loop
            if (i1 >= len1 || i2 >= len2)
            {
                break;
            }

            // Grab contiguous digit or alpha segment
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

            // First segment is guaranteed non-empty; if second is empty, types differ
            if (seg2.Length == 0)
            {
                return isNum ? 1 : -1;
            }

            if (isNum)
            {
                // Strip leading zeros
                var s1 = seg1.TrimStart('0');
                var s2 = seg2.TrimStart('0');
                if (s1.Length == 0)
                {
                    s1 = "";
                }

                if (s2.Length == 0)
                {
                    s2 = "";
                }

                // Longer number wins
                if (s1.Length != s2.Length)
                {
                    return s1.Length > s2.Length ? 1 : -1;
                }

                // Same length: compare lexicographically (works for digit strings)
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

        // Whichever has characters left is newer
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
