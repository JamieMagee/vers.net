using System;
using System.Collections.Generic;

namespace Vers.Schemes;

/// <summary>
/// Generic version comparer that splits on alpha/numeric boundaries
/// and compares segments numerically when both are numeric, otherwise lexicographically.
/// Used as the fallback for unknown versioning schemes.
/// </summary>
public sealed class GenericVersionComparer : IVersionComparer
{
    public static readonly GenericVersionComparer Instance = new();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var seg1 = Segment(version1);
        var seg2 = Segment(version2);

        int len = Math.Max(seg1.Count, seg2.Count);
        for (int i = 0; i < len; i++)
        {
            var s1 = i < seg1.Count ? seg1[i] : "";
            var s2 = i < seg2.Count ? seg2[i] : "";

            bool isNum1 = IsNumeric(s1);
            bool isNum2 = IsNumeric(s2);

            int cmp;
            if (isNum1 && isNum2)
            {
                cmp = CompareLargeIntegers(s1, s2);
            }
            else if (isNum1 != isNum2)
            {
                // Numeric segments sort before non-numeric
                cmp = isNum1 ? -1 : 1;
            }
            else
            {
                cmp = string.Compare(s1, s2, StringComparison.Ordinal);
            }

            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static List<string> Segment(string version)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(version))
        {
            return result;
        }

        int i = 0;
        while (i < version.Length)
        {
            // Skip separators (., -, _)
            if (version[i] == '.' || version[i] == '-' || version[i] == '_')
            {
                i++;
                continue;
            }

            int start = i;
            if (char.IsDigit(version[i]))
            {
                while (i < version.Length && char.IsDigit(version[i]))
                {
                    i++;
                }
            }
            else
            {
                while (
                    i < version.Length
                    && !char.IsDigit(version[i])
                    && version[i] != '.'
                    && version[i] != '-'
                    && version[i] != '_'
                )
                {
                    i++;
                }
            }

            result.Add(version.Substring(start, i - start));
        }

        return result;
    }

    private static bool IsNumeric(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }

        foreach (var c in s)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }
        return true;
    }

    private static int CompareLargeIntegers(string a, string b)
    {
        // Strip leading zeros
        a = a.TrimStart('0');
        b = b.TrimStart('0');
        if (a.Length == 0)
        {
            a = "0";
        }

        if (b.Length == 0)
        {
            b = "0";
        }

        if (a.Length != b.Length)
        {
            return a.Length.CompareTo(b.Length);
        }

        return string.Compare(a, b, StringComparison.Ordinal);
    }
}
