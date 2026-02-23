using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements Gentoo/Alpine (apk) version comparison.
///
/// Version format: [numeric segments][letter][suffixes][-rRevision]
///
/// Numeric segments are dot-separated integers compared numerically.
/// An optional single letter suffix (a-z) follows the last numeric segment.
/// Suffixes are _alpha, _beta, _pre, _rc, _p (with optional number).
/// Revision is -rN at the end.
///
/// Suffix ordering: _alpha &lt; _beta &lt; _pre &lt; _rc &lt; (none) &lt; _p
/// </summary>
public sealed class GentooVersionComparer : IVersionComparer
{
    public static readonly GentooVersionComparer Instance = new GentooVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var v1 = ParseGentoo(version1);
        var v2 = ParseGentoo(version2);

        // 1. Compare numeric segments
        int maxSegs = Math.Max(v1.NumericSegments.Length, v2.NumericSegments.Length);
        for (int i = 0; i < maxSegs; i++)
        {
            var a = i < v1.NumericSegments.Length ? v1.NumericSegments[i] : null;
            var b = i < v2.NumericSegments.Length ? v2.NumericSegments[i] : null;

            // Missing segment is NOT equal to 0 — it means fewer segments
            if (a == null && b == null)
            {
                break;
            }

            if (a == null)
            {
                return -1;
            }

            if (b == null)
            {
                return 1;
            }

            int cmp = CompareLargeIntegers(a, b);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        // 2. Compare letter suffix (no letter < letter, a < b < ... < z)
        int letterCmp = CompareLetter(v1.Letter, v2.Letter);
        if (letterCmp != 0)
        {
            return letterCmp;
        }

        // 3. Compare suffixes
        int maxSuf = Math.Max(v1.Suffixes.Length, v2.Suffixes.Length);
        for (int i = 0; i < maxSuf; i++)
        {
            var s1 = i < v1.Suffixes.Length ? v1.Suffixes[i] : GentooSuffix.None;
            var s2 = i < v2.Suffixes.Length ? v2.Suffixes[i] : GentooSuffix.None;

            int cmp = s1.CompareTo(s2);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        // 4. Compare revision
        return v1.Revision.CompareTo(v2.Revision);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static int CompareLetter(char a, char b)
    {
        if (a == b)
        {
            return 0;
        }
        // No letter (0) sorts before any letter
        if (a == '\0')
        {
            return -1;
        }

        if (b == '\0')
        {
            return 1;
        }

        return a.CompareTo(b);
    }

    private static int CompareLargeIntegers(string a, string b)
    {
        // Strip leading zeros for comparison
        var sa = a.TrimStart('0');
        var sb = b.TrimStart('0');
        if (sa.Length == 0)
        {
            sa = "0";
        }

        if (sb.Length == 0)
        {
            sb = "0";
        }

        if (sa.Length != sb.Length)
        {
            return sa.Length.CompareTo(sb.Length);
        }

        return string.Compare(sa, sb, StringComparison.Ordinal);
    }

    private struct GentooSuffix : IComparable<GentooSuffix>
    {
        // Ordering: alpha(0) < beta(1) < pre(2) < rc(3) < none(4) < p(5)
        public int Kind;
        public long Number;

        public static readonly GentooSuffix None = new GentooSuffix { Kind = 4, Number = 0 };

        public int CompareTo(GentooSuffix other)
        {
            int cmp = Kind.CompareTo(other.Kind);
            if (cmp != 0)
            {
                return cmp;
            }

            return Number.CompareTo(other.Number);
        }
    }

    private struct GentooParsed
    {
        public string[] NumericSegments;
        public char Letter;
        public GentooSuffix[] Suffixes;
        public long Revision;
    }

    private static GentooParsed ParseGentoo(string version)
    {
        var result = new GentooParsed
        {
            NumericSegments = [],
            Letter = '\0',
            Suffixes = [],
            Revision = 0,
        };

        int pos = 0;
        int len = version.Length;

        // 1. Parse numeric segments (dot-separated)
        var segments = new System.Collections.Generic.List<string>();
        while (pos < len && (char.IsDigit(version[pos]) || version[pos] == '.'))
        {
            if (version[pos] == '.')
            {
                pos++;
                continue;
            }

            int start = pos;
            while (pos < len && char.IsDigit(version[pos]))
            {
                pos++;
            }

            segments.Add(version.Substring(start, pos - start));
        }

        result.NumericSegments = segments.ToArray();

        // 2. Parse optional letter suffix (single lowercase letter before _ or -r or end)
        if (pos < len && char.IsLetter(version[pos]))
        {
            result.Letter = version[pos];
            pos++;
        }

        // 3. Parse suffixes (_alpha, _beta, _pre, _rc, _p with optional number)
        var suffixes = new System.Collections.Generic.List<GentooSuffix>();
        while (pos < len && version[pos] == '_')
        {
            pos++; // skip _
            var suffix = ParseSuffixTag(version, ref pos);
            suffixes.Add(suffix);
        }

        result.Suffixes = suffixes.ToArray();

        // 4. Parse revision (-rN)
        if (pos < len && version[pos] == '-' && pos + 1 < len && version[pos + 1] == 'r')
        {
            pos += 2; // skip -r
            int start = pos;
            while (pos < len && char.IsDigit(version[pos]))
            {
                pos++;
            }

            if (pos > start)
            {
                long.TryParse(
                    version.Substring(start, pos - start),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out result.Revision
                );
            }
        }

        return result;
    }

    private static GentooSuffix ParseSuffixTag(string version, ref int pos)
    {
        int start = pos;
        while (pos < version.Length && char.IsLetter(version[pos]))
        {
            pos++;
        }

        string tag = version.Substring(start, pos - start).ToLowerInvariant();

        long number = 0;
        int numStart = pos;
        while (pos < version.Length && char.IsDigit(version[pos]))
        {
            pos++;
        }

        if (pos > numStart)
        {
            long.TryParse(
                version.Substring(numStart, pos - numStart),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out number
            );
        }

        int kind = tag switch
        {
            "alpha" => 0,
            "beta" => 1,
            "pre" => 2,
            "rc" => 3,
            "p" => 5,
            _ => 4, // unknown treated as "none"
        };

        return new GentooSuffix { Kind = kind, Number = number };
    }
}
