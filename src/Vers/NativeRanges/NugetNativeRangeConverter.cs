using System;
using System.Collections.Generic;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts NuGet native range notation (math interval syntax) to vers and back.
/// Format: [1.0, 2.0) means >=1.0 and &lt;2.0
/// [ = inclusive, ( = exclusive, ] = inclusive, ) = exclusive
/// </summary>
public sealed class NugetNativeRangeConverter : INativeRangeConverter
{
    public static readonly NugetNativeRangeConverter Instance = new NugetNativeRangeConverter();

    public VersRange FromNative(string nativeRange)
    {
        if (string.IsNullOrWhiteSpace(nativeRange))
        {
            throw new VersException("Native range is empty.");
        }

        var trimmed = nativeRange.Trim();

        // Check if it's an interval notation
        if (
            (trimmed.StartsWith("[") || trimmed.StartsWith("("))
            && (trimmed.EndsWith("]") || trimmed.EndsWith(")"))
        )
        {
            return ParseInterval(trimmed);
        }

        // Otherwise it's a bare version (exact match)
        return new VersRange.Builder("nuget")
            .AddConstraint(Comparator.Equal, trimmed)
            .Build(VersioningSchemeRegistry.GetComparer("nuget"));
    }

    public string ToNative(VersRange range)
    {
        if (range.Constraints.Count == 1 && range.Constraints[0].Comparator == Comparator.Wildcard)
        {
            return "*";
        }

        if (range.Constraints.Count == 1 && range.Constraints[0].Comparator == Comparator.Equal)
        {
            return "[" + range.Constraints[0].Version + "]";
        }

        // Try to reconstruct interval notation
        VersionConstraint? lower = null;
        VersionConstraint? upper = null;

        foreach (var c in range.Constraints)
        {
            if (c.Comparator.IsGreater())
            {
                lower = c;
            }
            else if (c.Comparator.IsLesser())
            {
                upper = c;
            }
        }

        var lBracket = lower?.Comparator == Comparator.GreaterThanOrEqual ? "[" : "(";
        var rBracket = upper?.Comparator == Comparator.LessThanOrEqual ? "]" : ")";
        var lVer = lower?.Version ?? "";
        var rVer = upper?.Version ?? "";

        return $"{lBracket}{lVer}, {rVer}{rBracket}";
    }

    private static VersRange ParseInterval(string interval)
    {
        var lInclusive = interval[0] == '[';
        var rInclusive = interval[interval.Length - 1] == ']';
        var inner = interval.Substring(1, interval.Length - 2).Trim();
        var commaIdx = inner.IndexOf(',');

        var constraints = new List<VersionConstraint>();
        var comparer = VersioningSchemeRegistry.GetComparer("nuget");

        if (commaIdx < 0)
        {
            // Single version: [1.0] = exact match
            constraints.Add(new VersionConstraint(Comparator.Equal, inner.Trim()));
        }
        else
        {
            var low = inner.Substring(0, commaIdx).Trim();
            var high = inner.Substring(commaIdx + 1).Trim();

            if (!string.IsNullOrEmpty(low))
            {
                var comp = lInclusive ? Comparator.GreaterThanOrEqual : Comparator.GreaterThan;
                constraints.Add(new VersionConstraint(comp, low));
            }

            if (!string.IsNullOrEmpty(high))
            {
                var comp = rInclusive ? Comparator.LessThanOrEqual : Comparator.LessThan;
                constraints.Add(new VersionConstraint(comp, high));
            }
        }

        return new VersRange.Builder("nuget")
            .AddConstraints(constraints)
            .BuildSortedNoValidation(comparer);
    }
}
