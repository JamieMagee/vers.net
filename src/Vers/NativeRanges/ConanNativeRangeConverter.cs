using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts Conan native range notation to vers and back.
/// Supports: comparators, space-separated AND, tilde (~), caret (^)
/// </summary>
public sealed class ConanNativeRangeConverter : INativeRangeConverter
{
    public static readonly ConanNativeRangeConverter Instance = new();

    public VersRange FromNative(string nativeRange)
    {
        if (string.IsNullOrWhiteSpace(nativeRange))
        {
            throw new VersException("Native range is empty.");
        }

        var trimmed = nativeRange.Trim();

        // Strip trailing flags like ", include_prerelease=True"
        var flagIdx = trimmed.IndexOf(", include_prerelease", StringComparison.OrdinalIgnoreCase);
        if (flagIdx >= 0)
        {
            trimmed = trimmed.Substring(0, flagIdx).Trim();
        }

        // Handle pure wildcard (with optional trailing -)
        if (trimmed == "*" || trimmed == "*-")
        {
            return VersRange.Parse("vers:conan/>=0.0.0");
        }

        // Split on || (OR)
        var orParts = trimmed.Split(new[] { "||" }, StringSplitOptions.None);
        var allConstraints = new List<VersionConstraint>();

        foreach (var orPart in orParts)
        {
            var part = orPart.Trim();
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            var tokens = part.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var t = token.Trim();
                if (string.IsNullOrEmpty(t))
                {
                    continue;
                }

                allConstraints.AddRange(ParseToken(t));
            }
        }

        if (allConstraints.Count == 0)
        {
            throw new VersException("No constraints found in native range.");
        }

        var comparer = VersioningSchemeRegistry.GetComparer("conan");
        return new VersRange.Builder("conan")
            .AddConstraints(allConstraints)
            .BuildSortedNoValidation(comparer);
    }

    public string ToNative(VersRange range)
    {
        if (range.Constraints.Count == 1 && range.Constraints[0].Comparator == Comparator.Wildcard)
        {
            return "*";
        }

        return string.Join(
            " ",
            range.Constraints.Select(c =>
            {
                var op = c.Comparator switch
                {
                    Comparator.Equal => "",
                    Comparator.NotEqual => "!=",
                    Comparator.LessThan => "<",
                    Comparator.LessThanOrEqual => "<=",
                    Comparator.GreaterThan => ">",
                    Comparator.GreaterThanOrEqual => ">=",
                    _ => "",
                };
                return op + c.Version;
            })
        );
    }

    private static List<VersionConstraint> ParseToken(string token)
    {
        if (token.StartsWith("~"))
        {
            var ver = token.Substring(1).Trim();
            return ExpandTilde(StripTrailingDash(ver));
        }

        if (token.StartsWith("^"))
        {
            var ver = token.Substring(1).Trim();
            return ExpandCaret(StripTrailingDash(ver));
        }

        if (token.StartsWith(">="))
        {
            return
            [
                new VersionConstraint(Comparator.GreaterThanOrEqual, token.Substring(2).Trim()),
            ];
        }

        if (token.StartsWith("<="))
        {
            return [new VersionConstraint(Comparator.LessThanOrEqual, token.Substring(2).Trim())];
        }

        if (token.StartsWith("!="))
        {
            return [new VersionConstraint(Comparator.NotEqual, token.Substring(2).Trim())];
        }

        if (token.StartsWith("="))
        {
            return
            [
                new VersionConstraint(
                    Comparator.Equal,
                    StripTrailingDash(token.Substring(1).Trim())
                ),
            ];
        }

        if (token.StartsWith(">"))
        {
            return
            [
                new VersionConstraint(
                    Comparator.GreaterThan,
                    StripTrailingDash(token.Substring(1).Trim())
                ),
            ];
        }

        if (token.StartsWith("<"))
        {
            return [new VersionConstraint(Comparator.LessThan, token.Substring(1).Trim())];
        }

        // Bare version (possibly with trailing -)
        return [new VersionConstraint(Comparator.Equal, StripTrailingDash(token.Trim()))];
    }

    /// <summary>
    /// Conan tilde: ~2.5 → >=2.5, &lt;2.6-
    /// ~2.5.1 → >=2.5.1, &lt;2.6-
    /// The upper bound uses "-" suffix to sort before pre-releases.
    /// </summary>
    private static List<VersionConstraint> ExpandTilde(string version)
    {
        var parts = version.Split('.');
        // Increment the second segment (minor), drop the rest
        string upper;
        if (parts.Length >= 2)
        {
            upper = parts[0] + "." + Increment(parts[1]) + "-";
        }
        else
        {
            upper = Increment(parts[0]) + "-";
        }

        return
        [
            new VersionConstraint(Comparator.GreaterThanOrEqual, version),
            new VersionConstraint(Comparator.LessThan, upper),
        ];
    }

    /// <summary>
    /// Conan caret: ^2.5 → >=2.5, &lt;3-
    /// ^0.5 → >=0.5, &lt;0.6-
    /// ^0.0.5 → >=0.0.5, &lt;0.0.6-
    /// </summary>
    private static List<VersionConstraint> ExpandCaret(string version)
    {
        var parts = version.Split('.');
        string upper;

        if (parts[0] != "0")
        {
            upper = Increment(parts[0]) + "-";
        }
        else if (parts.Length >= 2 && parts[1] != "0")
        {
            upper = "0." + Increment(parts[1]) + "-";
        }
        else if (parts.Length >= 3)
        {
            upper = "0.0." + Increment(parts[2]) + "-";
        }
        else
        {
            upper = Increment(parts[0]) + "-";
        }

        return
        [
            new VersionConstraint(Comparator.GreaterThanOrEqual, version),
            new VersionConstraint(Comparator.LessThan, upper),
        ];
    }

    private static string Increment(string numStr)
    {
        if (long.TryParse(numStr, NumberStyles.None, CultureInfo.InvariantCulture, out long num))
        {
            return (num + 1).ToString(CultureInfo.InvariantCulture);
        }

        return numStr;
    }

    /// <summary>
    /// Strip trailing dash from version string used in comparators.
    /// Conan allows "1-" meaning version "1" (the dash is a pre-release marker syntax).
    /// </summary>
    private static string StripTrailingDash(string version)
    {
        if (version.EndsWith("-"))
        {
            return version.Substring(0, version.Length - 1);
        }

        return version;
    }
}
