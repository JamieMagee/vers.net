using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements RubyGems version comparison.
///
/// Versions are dot-separated segments. Each segment is either numeric
/// (compared as integers) or a string (compared lexicographically).
///
/// Key rules:
/// - Numeric segments compared as integers
/// - String segments compared lexicographically (case-sensitive)
/// - When comparing numeric vs string: numeric sorts AFTER string
///   (so 1.0.beta &lt; 1.0, making string segments pre-release)
/// - Missing trailing numeric segments are 0
/// - Missing trailing string segments sort after present string segments
/// </summary>
public sealed class GemVersionComparer : IVersionComparer
{
    public static readonly GemVersionComparer Instance = new GemVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var segs1 = ParseSegments(version1);
        var segs2 = ParseSegments(version2);

        int maxLen = Math.Max(segs1.Length, segs2.Length);
        for (int i = 0; i < maxLen; i++)
        {
            var s1 = i < segs1.Length ? segs1[i] : null;
            var s2 = i < segs2.Length ? segs2[i] : null;

            int cmp = CompareSegment(s1, s2);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static int CompareSegment(string? s1, string? s2)
    {
        bool isNum1 = s1 != null && IsNumeric(s1);
        bool isNum2 = s2 != null && IsNumeric(s2);

        // Both missing
        if (s1 == null && s2 == null)
        {
            return 0;
        }

        // One missing: depends on whether the other is numeric or string
        if (s1 == null)
        {
            // Missing vs present: if present is numeric, missing = 0
            // If present is string (pre-release), missing sorts AFTER (higher)
            return isNum2 ? (0L).CompareTo(ParseLong(s2!)) : 1;
        }

        if (s2 == null)
        {
            return isNum1 ? ParseLong(s1).CompareTo(0L) : -1;
        }

        // Both present
        if (isNum1 && isNum2)
        {
            return ParseLong(s1).CompareTo(ParseLong(s2));
        }

        // Type mismatch: numeric sorts AFTER string (string = pre-release)
        if (isNum1 != isNum2)
        {
            return isNum1 ? 1 : -1;
        }

        // Both strings: lexicographic
        return string.Compare(s1, s2, StringComparison.Ordinal);
    }

    private static string[] ParseSegments(string version)
    {
        // RubyGems splits on dots AND on transitions between digits and letters
        var result = new System.Collections.Generic.List<string>();
        int i = 0;

        while (i < version.Length)
        {
            if (version[i] == '.' || version[i] == '-')
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
                )
                {
                    i++;
                }
            }

            result.Add(version.Substring(start, i - start));
        }

        // Trim trailing zeros (RubyGems canonical form)
        while (result.Count > 0 && result[result.Count - 1] == "0")
        {
            result.RemoveAt(result.Count - 1);
        }

        return result.ToArray();
    }

    private static bool IsNumeric(string s)
    {
        foreach (var c in s)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return s.Length > 0;
    }

    private static long ParseLong(string s)
    {
        long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out long result);
        return result;
    }
}
