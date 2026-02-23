using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements OpenSSL version comparison.
///
/// Legacy format (pre-3.0): MAJOR.MINOR.PATCH[letter-suffix]
/// Examples: 0.9.8, 1.0.1h, 0.9.8ztl, 1.1.1ag
///
/// Modern format (3.0+): MAJOR.MINOR.PATCH (plain semver-style)
/// Examples: 3.0.0, 3.1.0, 4.1.11
///
/// Numeric segments are compared first, then the trailing letter suffix
/// is compared lexicographically (longer suffix > shorter when prefix matches).
/// </summary>
public sealed class OpensslVersionComparer : IVersionComparer
{
    public static readonly OpensslVersionComparer Instance = new OpensslVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        Parse(version1, out var segs1, out var letters1);
        Parse(version2, out var segs2, out var letters2);

        // Compare numeric segments
        int maxSegs = Math.Max(segs1.Length, segs2.Length);
        for (int i = 0; i < maxSegs; i++)
        {
            long a = i < segs1.Length ? segs1[i] : 0;
            long b = i < segs2.Length ? segs2[i] : 0;
            int cmp = a.CompareTo(b);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        // Compare letter suffix lexicographically
        return string.Compare(letters1, letters2, StringComparison.Ordinal);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static void Parse(string version, out long[] segments, out string letters)
    {
        // Split into dot-separated parts
        var parts = version.Split('.');
        var segList = new System.Collections.Generic.List<long>();
        letters = "";

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            // Find where digits end and letters begin
            int numEnd = 0;
            while (numEnd < part.Length && char.IsDigit(part[numEnd]))
            {
                numEnd++;
            }

            if (numEnd > 0)
            {
                long.TryParse(
                    part.Substring(0, numEnd),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long num
                );
                segList.Add(num);
            }

            // Trailing letters on the last segment (or any segment with letters)
            if (numEnd < part.Length)
            {
                letters = part.Substring(numEnd);
            }
        }

        segments = [.. segList];
    }
}
