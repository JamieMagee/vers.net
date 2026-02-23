using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts RubyGems native range notation to vers and back.
/// Supports: >=, <=, >, <, !=, =, ~> (pessimistic)
/// Multiple requirements are comma-separated (AND).
/// </summary>
public sealed class GemNativeRangeConverter : INativeRangeConverter
{
    public static readonly GemNativeRangeConverter Instance = new();

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

            constraints.AddRange(ParseRequirement(trimmed));
        }

        if (constraints.Count == 0)
        {
            throw new VersException("No requirements found in native range.");
        }

        var comparer = VersioningSchemeRegistry.GetComparer("gem");
        return new VersRange.Builder("gem")
            .AddConstraints(constraints)
            .BuildSortedNoValidation(comparer);
    }

    public string ToNative(VersRange range)
    {
        if (range.Constraints.Count == 1 && range.Constraints[0].Comparator == Comparator.Wildcard)
        {
            return ">= 0";
        }

        return string.Join(
            ", ",
            range.Constraints.Select(c =>
            {
                var op = c.Comparator switch
                {
                    Comparator.Equal => "= ",
                    Comparator.NotEqual => "!= ",
                    Comparator.LessThan => "< ",
                    Comparator.LessThanOrEqual => "<= ",
                    Comparator.GreaterThan => "> ",
                    Comparator.GreaterThanOrEqual => ">= ",
                    _ => "= ",
                };
                return op + c.Version;
            })
        );
    }

    private static List<VersionConstraint> ParseRequirement(string req)
    {
        var result = new List<VersionConstraint>();

        if (req.StartsWith("~>"))
        {
            // Pessimistic: ~>2.0.8 means >=2.0.8, <2.1
            var ver = req.Substring(2).Trim();
            result.AddRange(ExpandPessimistic(ver));
            return result;
        }

        if (req.StartsWith(">="))
        {
            result.Add(
                new VersionConstraint(Comparator.GreaterThanOrEqual, req.Substring(2).Trim())
            );
        }
        else if (req.StartsWith("<="))
        {
            result.Add(new VersionConstraint(Comparator.LessThanOrEqual, req.Substring(2).Trim()));
        }
        else if (req.StartsWith("!="))
        {
            result.Add(new VersionConstraint(Comparator.NotEqual, req.Substring(2).Trim()));
        }
        else if (req.StartsWith(">"))
        {
            result.Add(new VersionConstraint(Comparator.GreaterThan, req.Substring(1).Trim()));
        }
        else if (req.StartsWith("<"))
        {
            result.Add(new VersionConstraint(Comparator.LessThan, req.Substring(1).Trim()));
        }
        else if (req.StartsWith("="))
        {
            result.Add(new VersionConstraint(Comparator.Equal, req.Substring(1).Trim()));
        }
        else
        {
            result.Add(new VersionConstraint(Comparator.Equal, req.Trim()));
        }

        return result;
    }

    /// <summary>
    /// ~>2.0.8 → >=2.0.8, &lt;2.1
    /// ~>2.0 → >=2.0, &lt;3
    /// </summary>
    private static List<VersionConstraint> ExpandPessimistic(string version)
    {
        var parts = version.Split('.');
        if (parts.Length < 2)
        {
            throw new VersException(
                $"Pessimistic constraint requires at least two segments: ~>{version}"
            );
        }

        // Drop the last segment and increment the second-to-last
        var upperParts = new List<string>();
        for (int i = 0; i < parts.Length - 1; i++)
        {
            upperParts.Add(parts[i]);
        }

        var lastIdx = upperParts.Count - 1;
        if (
            long.TryParse(
                upperParts[lastIdx],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long num
            )
        )
        {
            upperParts[lastIdx] = (num + 1).ToString(CultureInfo.InvariantCulture);
        }

        return
        [
            new VersionConstraint(Comparator.GreaterThanOrEqual, version),
            new VersionConstraint(Comparator.LessThan, string.Join(".", upperParts)),
        ];
    }
}
