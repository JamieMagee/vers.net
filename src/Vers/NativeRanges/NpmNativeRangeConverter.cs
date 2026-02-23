using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts npm/node-semver native range notation to vers and back.
///
/// Supports: comparators (>=, <=, >, <, =), OR (||), tilde (~),
/// caret (^), hyphen ranges (X - Y), wildcards (*, x, X).
/// Space between comparators means AND.
/// </summary>
public sealed class NpmNativeRangeConverter : INativeRangeConverter
{
    public static readonly NpmNativeRangeConverter Instance = new NpmNativeRangeConverter();

    public VersRange FromNative(string nativeRange)
    {
        if (string.IsNullOrWhiteSpace(nativeRange))
        {
            throw new VersException("Native range is empty.");
        }

        var trimmed = nativeRange.Trim();

        // Handle pure wildcard
        if (trimmed == "*" || trimmed == "x" || trimmed == "X" || trimmed == "")
        {
            return VersRange.Parse("vers:npm/*");
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

            var constraints = ParseComparatorSet(part);
            allConstraints.AddRange(constraints);
        }

        if (allConstraints.Count == 0)
        {
            return VersRange.Parse("vers:npm/*");
        }

        // Normalize versions: strip v prefix, pad to 3 segments
        for (int j = 0; j < allConstraints.Count; j++)
        {
            var c = allConstraints[j];
            if (c.Comparator != Comparator.Wildcard)
            {
                allConstraints[j] = new VersionConstraint(
                    c.Comparator,
                    NormalizeNpmVersion(c.Version)
                );
            }
        }

        // Sort and build
        var comparer = VersioningSchemeRegistry.GetComparer("npm");
        var sorted = allConstraints
            .OrderBy(c => c.Version, Comparer<string>.Create((a, b) => comparer.Compare(a, b)))
            .ToList();

        return new VersRange.Builder("npm")
            .AddConstraints(sorted)
            .BuildSortedNoValidation(comparer);
    }

    public string ToNative(VersRange range)
    {
        if (range.Constraints.Count == 1 && range.Constraints[0].Comparator == Comparator.Wildcard)
        {
            return "*";
        }

        // Simple: output each constraint as native comparator
        var parts = new List<string>();
        foreach (var c in range.Constraints)
        {
            var prefix = c.Comparator switch
            {
                Comparator.Equal => "",
                Comparator.NotEqual => "!=",
                Comparator.LessThan => "<",
                Comparator.LessThanOrEqual => "<=",
                Comparator.GreaterThan => ">",
                Comparator.GreaterThanOrEqual => ">=",
                _ => "",
            };
            parts.Add(prefix + c.Version);
        }

        return string.Join(" ", parts);
    }

    private static List<VersionConstraint> ParseComparatorSet(string set)
    {
        var result = new List<VersionConstraint>();

        // Check for hyphen range: "X.Y.Z - A.B.C"
        var hyphenIdx = set.IndexOf(" - ", StringComparison.Ordinal);
        if (hyphenIdx > 0)
        {
            var low = set.Substring(0, hyphenIdx).Trim();
            var high = set.Substring(hyphenIdx + 3).Trim();
            result.Add(new VersionConstraint(Comparator.GreaterThanOrEqual, low));
            result.Add(new VersionConstraint(Comparator.LessThanOrEqual, high));
            return result;
        }

        // Split on whitespace (AND)
        var tokens = set.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int i = 0;
        while (i < tokens.Length)
        {
            var token = tokens[i].Trim();
            if (string.IsNullOrEmpty(token))
            {
                i++;
                continue;
            }

            if (token.StartsWith("~"))
            {
                var ver = token.Substring(1).TrimStart();
                result.AddRange(ExpandTilde(ver));
            }
            else if (token.StartsWith("^"))
            {
                var ver = token.Substring(1).TrimStart();
                result.AddRange(ExpandCaret(ver));
            }
            else if (token.StartsWith(">=") || token.StartsWith("<=") || token.StartsWith("!="))
            {
                var op = token.Substring(0, 2);
                var ver = token.Substring(2).TrimStart();
                // Version might be in the next token
                if (string.IsNullOrEmpty(ver) && i + 1 < tokens.Length)
                {
                    i++;
                    ver = tokens[i].Trim();
                }

                var comp = op switch
                {
                    ">=" => Comparator.GreaterThanOrEqual,
                    "<=" => Comparator.LessThanOrEqual,
                    "!=" => Comparator.NotEqual,
                    _ => Comparator.Equal,
                };

                // If version contains wildcard, expand it
                if (ContainsWildcard(ver))
                {
                    result.AddRange(ExpandWildcard(ver));
                }
                else
                {
                    result.Add(new VersionConstraint(comp, ver));
                }
            }
            else if (token.StartsWith(">") || token.StartsWith("<"))
            {
                var op = token.Substring(0, 1);
                var ver = token.Substring(1).TrimStart();
                if (string.IsNullOrEmpty(ver) && i + 1 < tokens.Length)
                {
                    i++;
                    ver = tokens[i].Trim();
                }

                var comp = op == ">" ? Comparator.GreaterThan : Comparator.LessThan;
                if (ContainsWildcard(ver))
                {
                    result.AddRange(ExpandWildcard(ver));
                }
                else
                {
                    result.Add(new VersionConstraint(comp, ver));
                }
            }
            else if (token.StartsWith("="))
            {
                var ver = token.Substring(1).TrimStart();
                if (string.IsNullOrEmpty(ver) && i + 1 < tokens.Length)
                {
                    i++;
                    ver = tokens[i].Trim();
                }

                result.Add(new VersionConstraint(Comparator.Equal, ver));
            }
            else
            {
                // Bare version or version with wildcards
                if (token == "*" || token == "x" || token == "X")
                {
                    // This OR branch matches everything — but we add nothing
                    // since it will be combined with other branches
                    return new List<VersionConstraint>
                    {
                        new VersionConstraint(Comparator.Wildcard, ""),
                    };
                }

                if (ContainsWildcard(token))
                {
                    result.AddRange(ExpandWildcard(token));
                }
                else
                {
                    result.Add(new VersionConstraint(Comparator.Equal, token));
                }
            }

            i++;
        }

        return result;
    }

    private static bool ContainsWildcard(string version)
    {
        var parts = version.Split('.');
        return parts.Any(p => p == "*" || p == "x" || p == "X");
    }

    /// <summary>
    /// Expands wildcard version: 2.0.x → >=2.0.0 &lt; 2.1.0
    /// </summary>
    private static List<VersionConstraint> ExpandWildcard(string version)
    {
        var parts = version.Split('.');
        var concrete = new List<string>();
        foreach (var p in parts)
        {
            if (p == "*" || p == "x" || p == "X")
            {
                break;
            }

            concrete.Add(p);
        }

        if (concrete.Count == 0)
        {
            return new List<VersionConstraint> { new VersionConstraint(Comparator.Wildcard, "") };
        }

        var low = string.Join(".", concrete) + ".0";
        while (low.Split('.').Length < 3)
        {
            low += ".0";
        }

        var upper = IncrementSegment(concrete);

        return new List<VersionConstraint>
        {
            new VersionConstraint(Comparator.GreaterThanOrEqual, low),
            new VersionConstraint(Comparator.LessThan, upper),
        };
    }

    /// <summary>
    /// Tilde: ~1.2.3 → >=1.2.3 &lt;1.3.0
    /// ~1.2 → >=1.2.0 &lt;1.3.0
    /// ~1 → >=1.0.0 &lt;2.0.0
    /// </summary>
    private static List<VersionConstraint> ExpandTilde(string version)
    {
        // Handle pre-release: ~0.8.0-pre → >=0.8.0-pre|<0.8.0|>=0.8.0|<0.8.1
        var dashIdx = version.IndexOf('-');
        if (dashIdx >= 0)
        {
            var mainPart = version.Substring(0, dashIdx);
            var parts = mainPart.Split('.');

            // For pre-release tilde, upper bound increments patch (not minor)
            string upper;
            if (parts.Length >= 3)
            {
                upper = parts[0] + "." + parts[1] + "." + Increment(parts[2]);
            }
            else if (parts.Length == 2)
            {
                upper = parts[0] + "." + Increment(parts[1]) + ".0";
            }
            else
            {
                upper = Increment(parts[0]) + ".0.0";
            }

            return new List<VersionConstraint>
            {
                new VersionConstraint(Comparator.GreaterThanOrEqual, version),
                new VersionConstraint(Comparator.LessThan, mainPart),
                new VersionConstraint(Comparator.GreaterThanOrEqual, mainPart),
                new VersionConstraint(Comparator.LessThan, upper),
            };
        }

        var tilParts = version.Split('.');
        var low = version;
        string tilUpper = ComputeTildeUpper(tilParts);

        return new List<VersionConstraint>
        {
            new VersionConstraint(Comparator.GreaterThanOrEqual, low),
            new VersionConstraint(Comparator.LessThan, tilUpper),
        };
    }

    /// <summary>
    /// Compute the upper bound for a tilde range.
    /// ~1 → &lt;2.0.0, ~1.2 → &lt;1.3.0, ~1.2.3 → &lt;1.3.0
    /// </summary>
    private static string ComputeTildeUpper(string[] parts)
    {
        if (parts.Length == 1)
        {
            return Increment(parts[0]) + ".0.0";
        }
        // For 2+ segments, increment minor
        return parts[0] + "." + Increment(parts[1]) + ".0";
    }

    /// <summary>
    /// Caret: ^1.2.3 → >=1.2.3 &lt;2.0.0
    /// ^0.2.3 → >=0.2.3 &lt;0.3.0
    /// ^0.0.3 → >=0.0.3 &lt;0.0.4
    /// </summary>
    private static List<VersionConstraint> ExpandCaret(string version)
    {
        // Split version, handling pre-release
        var dashIdx = version.IndexOf('-');
        string mainPart = dashIdx >= 0 ? version.Substring(0, dashIdx) : version;
        string? prerelease = dashIdx >= 0 ? version.Substring(dashIdx) : null;

        var parts = mainPart.Split('.');
        var low = version;

        string upper;
        if (parts.Length >= 1 && parts[0] != "0")
        {
            upper = Increment(parts[0]) + ".0.0";
        }
        else if (parts.Length >= 2 && parts[1] != "0")
        {
            upper = parts[0] + "." + Increment(parts[1]) + ".0";
        }
        else if (parts.Length >= 3)
        {
            upper = parts[0] + "." + parts[1] + "." + Increment(parts[2]);
        }
        else
        {
            upper = Increment(parts[0]) + ".0.0";
        }

        return new List<VersionConstraint>
        {
            new VersionConstraint(Comparator.GreaterThanOrEqual, low),
            new VersionConstraint(Comparator.LessThan, upper),
        };
    }

    private static string Increment(string numStr)
    {
        if (long.TryParse(numStr, NumberStyles.None, CultureInfo.InvariantCulture, out long num))
        {
            return (num + 1).ToString(CultureInfo.InvariantCulture);
        }

        return numStr;
    }

    private static string IncrementSegment(List<string> parts)
    {
        var last = parts.Count - 1;
        var incremented = new List<string>(parts);
        incremented[last] = Increment(incremented[last]);
        return string.Join(".", incremented) + ".0";
    }

    /// <summary>
    /// Normalize npm version: strip v/V prefix, pad to 3 segments (M.m.p).
    /// Preserves pre-release and build metadata.
    /// </summary>
    private static string NormalizeNpmVersion(string version)
    {
        var v = version;

        // Strip v prefix
        if (v.Length > 0 && (v[0] == 'v' || v[0] == 'V'))
        {
            v = v.Substring(1);
        }

        // Split off pre-release and build metadata
        string suffix = "";
        var dashIdx = v.IndexOf('-');
        var plusIdx = v.IndexOf('+');
        int suffixStart = -1;
        if (dashIdx >= 0 && (plusIdx < 0 || dashIdx < plusIdx))
        {
            suffixStart = dashIdx;
        }
        else if (plusIdx >= 0)
        {
            suffixStart = plusIdx;
        }

        if (suffixStart >= 0)
        {
            suffix = v.Substring(suffixStart);
            v = v.Substring(0, suffixStart);
        }

        // Pad to 3 segments
        var parts = v.Split('.');
        while (parts.Length < 3)
        {
            v += ".0";
            parts = v.Split('.');
        }

        return v + suffix;
    }
}
