using System;
using System.Collections.Generic;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts PyPI native range notation (PEP 508 version specifiers) to vers and back.
/// Format: comma-separated specifiers like ">=1.0,<2.0,!=1.5"
/// </summary>
public sealed class PypiNativeRangeConverter : INativeRangeConverter
{
    public static readonly PypiNativeRangeConverter Instance = new PypiNativeRangeConverter();

    public VersRange FromNative(string nativeRange)
    {
        if (string.IsNullOrWhiteSpace(nativeRange))
        {
            throw new VersException("Native range is empty.");
        }

        var constraints = new List<VersionConstraint>();
        var parts = nativeRange.Split(',');

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            constraints.Add(ParseSpecifier(trimmed));
        }

        if (constraints.Count == 0)
        {
            throw new VersException("No specifiers found in native range.");
        }

        var comparer = VersioningSchemeRegistry.GetComparer("pypi");
        return new VersRange.Builder("pypi")
            .AddConstraints(constraints)
            .BuildSortedNoValidation(comparer);
    }

    public string ToNative(VersRange range)
    {
        if (range.Constraints.Count == 1 && range.Constraints[0].Comparator == Comparator.Wildcard)
        {
            return "*";
        }

        return string.Join(
            ",",
            range.Constraints.Select(c =>
            {
                var op = c.Comparator switch
                {
                    Comparator.Equal => "==",
                    Comparator.NotEqual => "!=",
                    Comparator.LessThan => "<",
                    Comparator.LessThanOrEqual => "<=",
                    Comparator.GreaterThan => ">",
                    Comparator.GreaterThanOrEqual => ">=",
                    _ => "==",
                };
                return op + c.Version;
            })
        );
    }

    private static VersionConstraint ParseSpecifier(string spec)
    {
        if (spec.StartsWith(">="))
        {
            return new VersionConstraint(Comparator.GreaterThanOrEqual, spec.Substring(2).Trim());
        }

        if (spec.StartsWith("<="))
        {
            return new VersionConstraint(Comparator.LessThanOrEqual, spec.Substring(2).Trim());
        }

        if (spec.StartsWith("!="))
        {
            return new VersionConstraint(Comparator.NotEqual, spec.Substring(2).Trim());
        }

        if (spec.StartsWith("=="))
        {
            return new VersionConstraint(Comparator.Equal, spec.Substring(2).Trim());
        }

        if (spec.StartsWith(">"))
        {
            return new VersionConstraint(Comparator.GreaterThan, spec.Substring(1).Trim());
        }

        if (spec.StartsWith("<"))
        {
            return new VersionConstraint(Comparator.LessThan, spec.Substring(1).Trim());
        }

        // Bare version = equality
        return new VersionConstraint(Comparator.Equal, spec.Trim());
    }
}
