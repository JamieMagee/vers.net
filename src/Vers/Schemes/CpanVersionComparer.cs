using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements CPAN (Perl) version comparison.
///
/// Supports two formats:
/// - Decimal: "1.0203" → segments [1, 20, 300] (fractional part split into 3-digit groups)
/// - Dotted-decimal: "v1.2.3" or "1.2.3" (when 2+ dots) → segments [1, 2, 3]
///
/// Alpha versions (containing underscore) strip the underscore and compare
/// as the equivalent non-alpha version, but sort lower.
/// </summary>
public sealed class CpanVersionComparer : IVersionComparer
{
    public static readonly CpanVersionComparer Instance = new CpanVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var segs1 = ParseToSegments(version1, out bool alpha1);
        var segs2 = ParseToSegments(version2, out bool alpha2);

        int maxLen = Math.Max(segs1.Length, segs2.Length);
        for (int i = 0; i < maxLen; i++)
        {
            long a = i < segs1.Length ? segs1[i] : 0;
            long b = i < segs2.Length ? segs2[i] : 0;
            int cmp = a.CompareTo(b);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        // If versions are numerically equal, alpha sorts before non-alpha
        if (alpha1 != alpha2)
        {
            return alpha1 ? -1 : 1;
        }

        return 0;
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static long[] ParseToSegments(string version, out bool isAlpha)
    {
        isAlpha = version.IndexOf('_') >= 0;

        var s = version;

        // Strip leading 'v' or 'V' — forces dotted-decimal interpretation
        bool hasV = s.Length > 0 && (s[0] == 'v' || s[0] == 'V');
        if (hasV)
        {
            s = s.Substring(1);
        }

        // Remove underscores (alpha marker — affects sort only, not value)
        s = s.Replace("_", "");

        // Determine format: dotted-decimal if v-prefix OR 2+ dots, otherwise decimal
        int dotCount = 0;
        foreach (char c in s)
        {
            if (c == '.')
            {
                dotCount++;
            }
        }

        if (hasV || dotCount >= 2)
        {
            // Dotted-decimal: split on dots, parse each as integer
            return ParseDottedDecimal(s);
        }
        else
        {
            // Decimal: split fractional part into 3-digit groups
            return ParseDecimal(s);
        }
    }

    private static long[] ParseDottedDecimal(string s)
    {
        var parts = s.Split('.');
        var result = new long[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            long.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out result[i]);
        }

        return result;
    }

    private static long[] ParseDecimal(string s)
    {
        // Split on the single dot (if any)
        int dotIdx = s.IndexOf('.');
        string integerPart;
        string fractionalPart;

        if (dotIdx >= 0)
        {
            integerPart = s.Substring(0, dotIdx);
            fractionalPart = s.Substring(dotIdx + 1);
        }
        else
        {
            integerPart = s;
            fractionalPart = "";
        }

        var segments = new System.Collections.Generic.List<long>();

        // First segment is the integer part
        long.TryParse(
            integerPart.Length > 0 ? integerPart : "0",
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long intSeg
        );
        segments.Add(intSeg);

        // Split fractional part into 3-digit groups
        if (fractionalPart.Length > 0)
        {
            // Pad to multiple of 3
            while (fractionalPart.Length % 3 != 0)
            {
                fractionalPart += "0";
            }

            for (int i = 0; i < fractionalPart.Length; i += 3)
            {
                var chunk = fractionalPart.Substring(i, 3);
                long.TryParse(chunk, NumberStyles.None, CultureInfo.InvariantCulture, out long seg);
                segments.Add(seg);
            }
        }

        return segments.ToArray();
    }
}
