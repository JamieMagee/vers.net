using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Compares integer dot-separated versions such as "10.234.5.12".
/// Only non-negative integers separated by dots. Leading zeros are ignored.
/// Interpretation stops at the first character that is not a digit or dot.
/// </summary>
public sealed class IntdotVersionComparer : IVersionComparer
{
    public static readonly IntdotVersionComparer Instance = new();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var seg1 = ParseSegments(version1);
        var seg2 = ParseSegments(version2);

        int len = Math.Max(seg1.Length, seg2.Length);
        for (int i = 0; i < len; i++)
        {
            long a = i < seg1.Length ? seg1[i] : 0;
            long b = i < seg2.Length ? seg2[i] : 0;
            int cmp = a.CompareTo(b);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return false;
        }

        foreach (var c in version)
        {
            if (!char.IsDigit(c) && c != '.')
            {
                return false;
            }
        }
        return true;
    }

    private static long[] ParseSegments(string version)
    {
        // Stop at first non-digit, non-dot character
        int end = 0;
        while (end < version.Length && (char.IsDigit(version[end]) || version[end] == '.'))
        {
            end++;
        }

        var s = version.Substring(0, end);
        if (string.IsNullOrEmpty(s))
        {
            return [];
        }

        var parts = s.Split('.');
        var result = new long[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
            {
                result[i] = 0;
            }
            else
            {
                long.TryParse(
                    parts[i],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out result[i]
                );
            }
        }
        return result;
    }
}
