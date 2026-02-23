using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts nginx native range notation to vers and back.
/// Formats:
///   "0.9.10" → vers:nginx/0.9.10 (exact version)
///   "0.8.40+" → vers:nginx/>=0.8.40|&lt;0.9.0 (version and later within same minor)
///   "0.7.52-0.8.39" → vers:nginx/>=0.7.52|&lt;=0.8.39 (range)
///   "1.5.0+, 1.4.1+" → comma-separated, combined
/// </summary>
public sealed class NginxNativeRangeConverter : INativeRangeConverter
{
    public static readonly NginxNativeRangeConverter Instance = new NginxNativeRangeConverter();

    public VersRange FromNative(string nativeRange)
    {
        if (string.IsNullOrWhiteSpace(nativeRange))
        {
            throw new VersException("Native range is empty.");
        }

        var rawParts = nativeRange.Split(',');
        var parsedParts = new List<List<VersionConstraint>>();

        foreach (var part in rawParts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            parsedParts.Add(ParsePart(trimmed));
        }

        // For multiple "+" ranges, sort by version and only add upper bound
        // where the next range provides a natural ceiling
        var allConstraints = new List<VersionConstraint>();
        var plusRanges = new List<(string Version, int Order)>();
        var nonPlusRanges = new List<List<VersionConstraint>>();

        for (int i = 0; i < parsedParts.Count; i++)
        {
            var pp = parsedParts[i];
            var rawPart = rawParts[i].Trim();
            if (rawPart.EndsWith("+") && pp.Count >= 1)
            {
                plusRanges.Add((pp[0].Version, i));
            }
            else
            {
                nonPlusRanges.Add(pp);
            }
        }

        if (plusRanges.Count > 1)
        {
            // Sort by version
            var comparer = VersioningSchemeRegistry.GetComparer("nginx");
            plusRanges.Sort((a, b) => comparer.Compare(a.Version, b.Version));

            for (int i = 0; i < plusRanges.Count; i++)
            {
                allConstraints.Add(
                    new VersionConstraint(Comparator.GreaterThanOrEqual, plusRanges[i].Version)
                );
                if (i < plusRanges.Count - 1)
                {
                    allConstraints.Add(
                        new VersionConstraint(Comparator.LessThan, plusRanges[i + 1].Version)
                    );
                }
            }

            foreach (var pp in nonPlusRanges)
            {
                allConstraints.AddRange(pp);
            }
        }
        else
        {
            foreach (var pp in parsedParts)
            {
                allConstraints.AddRange(pp);
            }
        }

        if (allConstraints.Count == 0)
        {
            throw new VersException("No constraints found in native range.");
        }

        var cmp = VersioningSchemeRegistry.GetComparer("nginx");
        return new VersRange.Builder("nginx")
            .AddConstraints(allConstraints)
            .BuildSortedNoValidation(cmp);
    }

    public string ToNative(VersRange range)
    {
        return string.Join(
            ", ",
            range.Constraints.Select(c =>
            {
                var op = c.Comparator switch
                {
                    Comparator.Equal => "",
                    Comparator.GreaterThanOrEqual => ">=",
                    Comparator.LessThanOrEqual => "<=",
                    Comparator.LessThan => "<",
                    Comparator.GreaterThan => ">",
                    _ => "",
                };
                return op + c.Version;
            })
        );
    }

    private static List<VersionConstraint> ParsePart(string part)
    {
        var result = new List<VersionConstraint>();

        if (part.EndsWith("+"))
        {
            // "0.8.40+" → >=0.8.40|<0.9.0
            var ver = part.Substring(0, part.Length - 1);
            result.Add(new VersionConstraint(Comparator.GreaterThanOrEqual, ver));

            // Compute upper bound: increment minor, reset patch to 0
            var segments = ver.Split('.');
            if (segments.Length >= 2)
            {
                var upper = segments[0] + "." + Increment(segments[1]) + ".0";
                result.Add(new VersionConstraint(Comparator.LessThan, upper));
            }

            return result;
        }

        // Check for hyphen range: "0.7.52-0.8.39"
        // Careful: hyphens also appear in version numbers, so look for the pattern
        // where we have version-version (two dot-separated numbers joined by a single hyphen)
        var dashIdx = FindRangeDash(part);
        if (dashIdx > 0)
        {
            var low = part.Substring(0, dashIdx);
            var high = part.Substring(dashIdx + 1);
            result.Add(new VersionConstraint(Comparator.GreaterThanOrEqual, low));
            result.Add(new VersionConstraint(Comparator.LessThanOrEqual, high));
            return result;
        }

        // Bare version
        result.Add(new VersionConstraint(Comparator.Equal, part));
        return result;
    }

    /// <summary>
    /// Find the dash that separates two versions in "X.Y.Z-A.B.C".
    /// The dash must be preceded by a digit and followed by a digit.
    /// </summary>
    private static int FindRangeDash(string part)
    {
        for (int i = 1; i < part.Length - 1; i++)
        {
            if (part[i] == '-' && char.IsDigit(part[i - 1]) && char.IsDigit(part[i + 1]))
            {
                // Verify it's not just a version segment by checking there's a dot after
                var after = part.Substring(i + 1);
                if (after.Contains('.'))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string Increment(string numStr)
    {
        if (long.TryParse(numStr, NumberStyles.None, CultureInfo.InvariantCulture, out long num))
        {
            return (num + 1).ToString(CultureInfo.InvariantCulture);
        }

        return numStr;
    }
}
