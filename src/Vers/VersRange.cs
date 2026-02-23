using System;
using System.Collections.Generic;
using System.Linq;

namespace Vers;

/// <summary>
/// Represents a parsed vers (VErsion Range Specifier) string.
/// Immutable. Use <see cref="Parse"/> to create from a string,
/// or <see cref="Builder"/> to construct programmatically.
/// </summary>
public sealed class VersRange : IEquatable<VersRange>
{
    public string Scheme { get; }
    public IReadOnlyList<VersionConstraint> Constraints { get; }

    private VersRange(string scheme, IReadOnlyList<VersionConstraint> constraints)
    {
        Scheme = scheme;
        Constraints = constraints;
    }

    // -----------------------------------------------------------------
    //  Parsing
    // -----------------------------------------------------------------

    /// <summary>
    /// Parses a vers string such as "vers:npm/&gt;=1.0.0|&lt;3.0.0".
    /// </summary>
    public static VersRange Parse(string versString)
    {
        if (versString == null)
        {
            throw new ArgumentNullException(nameof(versString));
        }

        // Strip spaces and tabs
        var s = StripWhitespace(versString);

        // Split on first ':'
        var colonIdx = s.IndexOf(':');
        if (colonIdx < 0)
        {
            throw new VersException($"Invalid vers string: missing ':' in '{versString}'.");
        }

        var uriScheme = s.Substring(0, colonIdx);
        if (!uriScheme.Equals("vers", StringComparison.OrdinalIgnoreCase))
        {
            throw new VersException(
                $"Invalid vers URI scheme: expected 'vers' but got '{uriScheme}'."
            );
        }

        var specifier = s.Substring(colonIdx + 1);

        // Split on first '/'
        var slashIdx = specifier.IndexOf('/');
        if (slashIdx < 0)
        {
            throw new VersException(
                $"Invalid vers string: missing '/' after versioning scheme in '{versString}'."
            );
        }

        var scheme = specifier.Substring(0, slashIdx).ToLowerInvariant();
        if (string.IsNullOrEmpty(scheme))
        {
            throw new VersException(
                $"Invalid vers string: versioning scheme is empty in '{versString}'."
            );
        }

        var constraintsStr = specifier.Substring(slashIdx + 1);
        if (string.IsNullOrEmpty(constraintsStr))
        {
            throw new VersException(
                $"Invalid vers string: constraints are empty in '{versString}'."
            );
        }

        // Enforce none/all schemes: only vers:none/* and vers:all/* are valid
        if (scheme == "none" || scheme == "all")
        {
            if (constraintsStr != "*")
            {
                throw new VersException(
                    $"Invalid vers string: scheme '{scheme}' only allows wildcard '*' constraint."
                );
            }
        }

        // Handle wildcard
        if (constraintsStr == "*")
        {
            return new VersRange(scheme, new[] { new VersionConstraint(Comparator.Wildcard, "") });
        }

        // Strip leading/trailing pipes, split on pipe
        constraintsStr = constraintsStr.Trim('|');
        var parts = SplitOnPipe(constraintsStr);

        if (parts.Count == 0)
        {
            throw new VersException(
                $"Invalid vers string: no constraints found in '{versString}'."
            );
        }

        var constraints = new List<VersionConstraint>(parts.Count);
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            constraints.Add(VersionConstraint.Parse(part));
        }

        if (constraints.Count == 0)
        {
            throw new VersException(
                $"Invalid vers string: no constraints found in '{versString}'."
            );
        }

        // Wildcard must appear alone — reject at parse time
        if (constraints.Any(c => c.Comparator == Comparator.Wildcard))
        {
            if (constraints.Count > 1)
            {
                throw new VersException(
                    "Invalid vers string: wildcard '*' must appear alone without other constraints."
                );
            }

            return new VersRange(scheme, constraints.AsReadOnly());
        }

        return new VersRange(scheme, constraints.AsReadOnly());
    }

    // -----------------------------------------------------------------
    //  Containment
    // -----------------------------------------------------------------

    /// <summary>
    /// Tests whether the given version is contained within this range,
    /// using the version comparer registered for this scheme.
    /// </summary>
    public bool Contains(string version)
    {
        var comparer = VersioningSchemeRegistry.GetComparer(Scheme);
        return Contains(version, comparer);
    }

    /// <summary>
    /// Tests whether the given version is contained within this range
    /// using the specified version comparer.
    /// </summary>
    public bool Contains(string version, IVersionComparer comparer)
    {
        if (version == null)
        {
            throw new ArgumentNullException(nameof(version));
        }

        if (comparer == null)
        {
            throw new ArgumentNullException(nameof(comparer));
        }

        var constraints = Constraints;

        // Single wildcard → always in range (except "none" which contains no versions)
        if (constraints.Count == 1 && constraints[0].Comparator == Comparator.Wildcard)
        {
            return Scheme != "none";
        }

        // Check equality comparators (=, <=, >=)
        foreach (var c in constraints)
        {
            if (
                c.Comparator == Comparator.Equal
                || c.Comparator == Comparator.LessThanOrEqual
                || c.Comparator == Comparator.GreaterThanOrEqual
            )
            {
                if (comparer.Compare(version, c.Version) == 0)
                {
                    return true;
                }
            }
        }

        // Check != exclusions
        foreach (var c in constraints)
        {
            if (c.Comparator == Comparator.NotEqual && comparer.Compare(version, c.Version) == 0)
            {
                return false;
            }
        }

        // Build the list of range constraints (not = and not !=)
        var rangeConstraints = new List<VersionConstraint>();
        foreach (var c in constraints)
        {
            if (c.Comparator != Comparator.Equal && c.Comparator != Comparator.NotEqual)
            {
                rangeConstraints.Add(c);
            }
        }

        if (rangeConstraints.Count == 0)
        {
            return false;
        }

        // Sort range constraints by version
        rangeConstraints.Sort((a, b) => comparer.Compare(a.Version, b.Version));

        // Single range constraint (e.g., ">=2.0.0" or "<3.0.0" alone)
        if (rangeConstraints.Count == 1)
        {
            var single = rangeConstraints[0];
            return single.Matches(version, comparer);
        }

        // Pairwise iteration
        for (int i = 0; i < rangeConstraints.Count - 1; i++)
        {
            var current = rangeConstraints[i];
            var next = rangeConstraints[i + 1];

            // First iteration: if current is < or <=, check if version < current.version
            if (i == 0 && current.Comparator.IsLesser())
            {
                bool inRange =
                    current.Comparator == Comparator.LessThan
                        ? comparer.Compare(version, current.Version) < 0
                        : comparer.Compare(version, current.Version) <= 0;
                if (inRange)
                {
                    return true;
                }
            }

            // Last iteration: if next is > or >=, check if version > next.version
            if (i == rangeConstraints.Count - 2 && next.Comparator.IsGreater())
            {
                bool inRange =
                    next.Comparator == Comparator.GreaterThan
                        ? comparer.Compare(version, next.Version) > 0
                        : comparer.Compare(version, next.Version) >= 0;
                if (inRange)
                {
                    return true;
                }
            }

            // If current is > or >= and next is < or <=, check interval
            if (current.Comparator.IsGreater() && next.Comparator.IsLesser())
            {
                bool aboveCurrent =
                    current.Comparator == Comparator.GreaterThan
                        ? comparer.Compare(version, current.Version) > 0
                        : comparer.Compare(version, current.Version) >= 0;
                bool belowNext =
                    next.Comparator == Comparator.LessThan
                        ? comparer.Compare(version, next.Version) < 0
                        : comparer.Compare(version, next.Version) <= 0;
                if (aboveCurrent && belowNext)
                {
                    return true;
                }
            }

            // If current is < or <= and next is > or >=, these versions are outside the range
            // Continue to next iteration
        }

        return false;
    }

    // -----------------------------------------------------------------
    //  Validation
    // -----------------------------------------------------------------

    /// <summary>
    /// Validates this range per the vers spec rules. Throws <see cref="VersException"/>
    /// if invalid. Returns this instance if valid.
    /// </summary>
    public VersRange Validate()
    {
        return Validate(VersioningSchemeRegistry.GetComparer(Scheme));
    }

    public VersRange Validate(IVersionComparer comparer)
    {
        var constraints = Constraints;

        // Single wildcard is always valid
        if (constraints.Count == 1 && constraints[0].Comparator == Comparator.Wildcard)
        {
            return this;
        }

        // Wildcard must be alone
        if (constraints.Any(c => c.Comparator == Comparator.Wildcard))
        {
            throw new VersException("Wildcard '*' must appear alone without other constraints.");
        }

        // Unique versions
        var versions = new HashSet<string>();
        foreach (var c in constraints)
        {
            if (!versions.Add(c.Version))
            {
                throw new VersException($"Duplicate version '{c.Version}' in constraints.");
            }
        }

        // Verify sorted by version
        for (int i = 1; i < constraints.Count; i++)
        {
            if (comparer.Compare(constraints[i - 1].Version, constraints[i].Version) > 0)
            {
                throw new VersException(
                    $"Constraints are not sorted by version: '{constraints[i - 1].Version}' should come before '{constraints[i].Version}'."
                );
            }
        }

        // Comparator alternation rules (ignoring != and =):
        // < and <= must be followed by > or >= (or nothing)
        // > and >= must be followed by < or <= (or nothing)
        var filtered = constraints
            .Where(c => c.Comparator != Comparator.NotEqual && c.Comparator != Comparator.Equal)
            .ToList();
        for (int i = 0; i < filtered.Count - 1; i++)
        {
            var curr = filtered[i].Comparator;
            var next = filtered[i + 1].Comparator;

            if (curr.IsLesser() && !next.IsGreater())
            {
                throw new VersException(
                    $"Invalid comparator sequence: '{curr.ToSymbol()}' followed by '{next.ToSymbol()}'. Expected '>' or '>='."
                );
            }

            if (curr.IsGreater() && !next.IsLesser())
            {
                throw new VersException(
                    $"Invalid comparator sequence: '{curr.ToSymbol()}' followed by '{next.ToSymbol()}'. Expected '<' or '<='."
                );
            }
        }

        // = must be followed by =, >, >= (or nothing) — ignoring !=
        var nonNotEqual = constraints.Where(c => c.Comparator != Comparator.NotEqual).ToList();
        for (int i = 0; i < nonNotEqual.Count - 1; i++)
        {
            var curr = nonNotEqual[i].Comparator;
            var next = nonNotEqual[i + 1].Comparator;
            if (curr == Comparator.Equal && next != Comparator.Equal && !next.IsGreater())
            {
                throw new VersException(
                    $"Invalid comparator sequence: '=' followed by '{next.ToSymbol()}'. Expected '=', '>' or '>='."
                );
            }
        }

        return this;
    }

    // -----------------------------------------------------------------
    //  Simplification
    // -----------------------------------------------------------------

    /// <summary>
    /// Returns a simplified version of this range per the vers spec algorithm.
    /// </summary>
    public VersRange Simplify()
    {
        return Simplify(VersioningSchemeRegistry.GetComparer(Scheme));
    }

    public VersRange Simplify(IVersionComparer comparer)
    {
        if (Constraints.Count <= 1)
        {
            return this;
        }

        // Split into != and remainder
        var notEquals = Constraints.Where(c => c.Comparator == Comparator.NotEqual).ToList();
        var remainder = Constraints.Where(c => c.Comparator != Comparator.NotEqual).ToList();

        if (remainder.Count == 0)
        {
            return this;
        }

        // Iterate and remove redundant constraints
        var result = new List<VersionConstraint>(remainder);
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < result.Count - 1; i++)
            {
                var curr = result[i];
                var next = result[i + 1];

                // If current is >, >= and next is =, >, >= → discard next
                if (
                    curr.Comparator.IsGreater()
                    && (next.Comparator == Comparator.Equal || next.Comparator.IsGreater())
                )
                {
                    result.RemoveAt(i + 1);
                    changed = true;
                    break;
                }

                // If current is =, <, <= and next is <, <= → discard current
                if (
                    (curr.Comparator == Comparator.Equal || curr.Comparator.IsLesser())
                    && next.Comparator.IsLesser()
                )
                {
                    result.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }

        // Concatenate != and filtered remainder, sort by version
        var combined = new List<VersionConstraint>(notEquals.Count + result.Count);
        combined.AddRange(notEquals);
        combined.AddRange(result);
        combined.Sort((a, b) => comparer.Compare(a.Version, b.Version));

        return new VersRange(Scheme, combined.AsReadOnly());
    }

    // -----------------------------------------------------------------
    //  Canonical string
    // -----------------------------------------------------------------

    public override string ToString()
    {
        if (Constraints.Count == 1 && Constraints[0].Comparator == Comparator.Wildcard)
        {
            return $"vers:{Scheme}/*";
        }

        return $"vers:{Scheme}/{string.Join("|", Constraints.Select(c => c.ToString()))}";
    }

    // -----------------------------------------------------------------
    //  Builder
    // -----------------------------------------------------------------

    public sealed class Builder
    {
        private readonly string _scheme;
        private readonly List<VersionConstraint> _constraints = [];

        public Builder(string scheme)
        {
            _scheme = scheme?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(scheme));
        }

        public Builder AddConstraint(Comparator comparator, string version)
        {
            _constraints.Add(new VersionConstraint(comparator, version));
            return this;
        }

        public Builder AddWildcard()
        {
            _constraints.Add(new VersionConstraint(Comparator.Wildcard, ""));
            return this;
        }

        public VersRange Build()
        {
            return Build(VersioningSchemeRegistry.GetComparer(_scheme));
        }

        public VersRange Build(IVersionComparer comparer)
        {
            if (_constraints.Count == 0)
            {
                throw new VersException("Cannot build a vers with no constraints.");
            }

            // Sort by version
            var sorted = _constraints.ToList();
            if (!(sorted.Count == 1 && sorted[0].Comparator == Comparator.Wildcard))
            {
                sorted.Sort((a, b) => comparer.Compare(a.Version, b.Version));
            }

            var result = new VersRange(_scheme, sorted.AsReadOnly());
            result.Validate(comparer);
            return result;
        }
    }

    // -----------------------------------------------------------------
    //  Equality
    // -----------------------------------------------------------------

    public bool Equals(VersRange? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Scheme != other.Scheme)
        {
            return false;
        }

        if (Constraints.Count != other.Constraints.Count)
        {
            return false;
        }

        for (int i = 0; i < Constraints.Count; i++)
        {
            if (!Constraints[i].Equals(other.Constraints[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as VersRange);

    public override int GetHashCode()
    {
        int hash = Scheme.GetHashCode();
        foreach (var c in Constraints)
        {
            hash ^= c.GetHashCode();
        }

        return hash;
    }

    // -----------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------

    private static string StripWhitespace(string s)
    {
        if (s.IndexOf(' ') < 0 && s.IndexOf('\t') < 0)
        {
            return s;
        }

        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c != ' ' && c != '\t')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static List<string> SplitOnPipe(string s)
    {
        var result = new List<string>();
        var parts = s.Split('|');
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part))
            {
                result.Add(part);
            }
        }
        return result;
    }
}
