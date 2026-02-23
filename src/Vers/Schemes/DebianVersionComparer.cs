using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements Debian version comparison per Debian Policy §5.6.12.
///
/// Format: [epoch:]upstream_version[-debian_revision]
///
/// Epoch is compared numerically (default 0).
/// upstream_version and debian_revision are compared using dpkg's algorithm:
/// alternating non-digit and digit segments compared left to right.
/// Non-digit segments: lexical where letters sort before non-letters,
/// and ~ sorts before everything (including empty/end of string).
/// Digit segments: compared numerically (empty = 0).
/// </summary>
public sealed class DebianVersionComparer : IVersionComparer
{
    public static readonly DebianVersionComparer Instance = new DebianVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        SplitComponents(version1, out long epoch1, out string upstream1, out string revision1);
        SplitComponents(version2, out long epoch2, out string upstream2, out string revision2);

        int cmp = epoch1.CompareTo(epoch2);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = CompareFragment(upstream1, upstream2);
        if (cmp != 0)
        {
            return cmp;
        }

        return CompareFragment(revision1, revision2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static void SplitComponents(
        string version,
        out long epoch,
        out string upstream,
        out string revision
    )
    {
        epoch = 0;
        upstream = version;
        revision = "0";

        // Extract epoch
        int colonIdx = upstream.IndexOf(':');
        if (colonIdx > 0)
        {
            long.TryParse(
                upstream.Substring(0, colonIdx),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out epoch
            );
            upstream = upstream.Substring(colonIdx + 1);
        }

        // Extract debian_revision (after LAST hyphen)
        int dashIdx = upstream.LastIndexOf('-');
        if (dashIdx >= 0)
        {
            revision = upstream.Substring(dashIdx + 1);
            upstream = upstream.Substring(0, dashIdx);
        }
    }

    /// <summary>
    /// Compares two version fragments using dpkg's algorithm.
    /// Alternates between non-digit and digit segments.
    /// </summary>
    private static int CompareFragment(string a, string b)
    {
        int i = 0,
            j = 0;

        while (i < a.Length || j < b.Length)
        {
            // Compare non-digit segment
            int cmp = CompareNonDigit(a, ref i, b, ref j);
            if (cmp != 0)
            {
                return cmp;
            }

            // Compare digit segment
            cmp = CompareDigit(a, ref i, b, ref j);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    /// <summary>
    /// Compares the non-digit prefix at the current positions.
    /// Letters sort before non-letters. ~ sorts before everything (even end of string).
    /// </summary>
    private static int CompareNonDigit(string a, ref int i, string b, ref int j)
    {
        while ((i < a.Length && !char.IsDigit(a[i])) || (j < b.Length && !char.IsDigit(b[j])))
        {
            int ca = i < a.Length && !char.IsDigit(a[i]) ? CharOrder(a[i]) : 0;
            int cb = j < b.Length && !char.IsDigit(b[j]) ? CharOrder(b[j]) : 0;

            if (ca != cb)
            {
                return ca.CompareTo(cb);
            }

            if (i < a.Length && !char.IsDigit(a[i]))
            {
                i++;
            }

            if (j < b.Length && !char.IsDigit(b[j]))
            {
                j++;
            }
        }

        return 0;
    }

    /// <summary>
    /// Returns the sort order for a non-digit character per dpkg rules:
    /// ~ sorts first (before empty), letters sort before non-letters.
    /// </summary>
    private static int CharOrder(char c)
    {
        if (c == '~')
        {
            return -1;
        }

        if (char.IsLetter(c))
        {
            return c;
        }
        // Non-letter, non-tilde: sorts after all letters
        // Letters are in range 65-122, so add 256 to put non-letters after
        return c + 256;
    }

    /// <summary>
    /// Compares the digit prefix at the current positions numerically.
    /// Empty = 0.
    /// </summary>
    private static int CompareDigit(string a, ref int i, string b, ref int j)
    {
        // Skip leading zeros
        while (i < a.Length && a[i] == '0')
        {
            i++;
        }

        while (j < b.Length && b[j] == '0')
        {
            j++;
        }

        // Count remaining digits
        int startA = i,
            startB = j;
        while (i < a.Length && char.IsDigit(a[i]))
        {
            i++;
        }

        while (j < b.Length && char.IsDigit(b[j]))
        {
            j++;
        }

        int lenA = i - startA;
        int lenB = j - startB;

        // Longer number is greater
        if (lenA != lenB)
        {
            return lenA.CompareTo(lenB);
        }

        // Same length: compare digit by digit
        for (int k = 0; k < lenA; k++)
        {
            if (a[startA + k] != b[startB + k])
            {
                return a[startA + k].CompareTo(b[startB + k]);
            }
        }

        return 0;
    }
}
