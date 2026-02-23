using System;
using System.Collections.Generic;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements Apache Maven version comparison as specified by
/// org.apache.maven.artifact.versioning.ComparableVersion.
/// Versions are split into items separated by '.', '-', or digit/letter transitions.
/// Qualifiers have a special ordering: alpha &lt; beta &lt; milestone &lt; rc/cr &lt; snapshot &lt; "" &lt; sp.
/// Unknown qualifiers sort after "sp" in lexicographic order.
/// </summary>
public sealed class MavenVersionComparer : IVersionComparer
{
    public static readonly MavenVersionComparer Instance = new();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var items1 = ParseVersion(version1.ToLowerInvariant());
        var items2 = ParseVersion(version2.ToLowerInvariant());

        return CompareItems(items1, items2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    private static int CompareItems(List<object> items1, List<object> items2)
    {
        int len = Math.Max(items1.Count, items2.Count);
        for (int i = 0; i < len; i++)
        {
            var left = i < items1.Count ? items1[i] : null;
            var right = i < items2.Count ? items2[i] : null;

            int cmp = CompareItem(left, right);
            if (cmp != 0)
            {
                return cmp;
            }
        }
        return 0;
    }

    private static int CompareItem(object? left, object? right)
    {
        // null padding: treated as "0" for int, "" for string, empty for list
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            if (right is long l)
            {
                return 0L.CompareTo(l);
            }

            if (right is string s)
            {
                return CompareQualifier("", s);
            }

            if (right is List<object> lst)
            {
                return CompareItems([], lst);
            }

            return 0;
        }

        if (right == null)
        {
            if (left is long l)
            {
                return l.CompareTo(0L);
            }

            if (left is string s)
            {
                return CompareQualifier(s, "");
            }

            if (left is List<object> lst)
            {
                return CompareItems(lst, []);
            }

            return 0;
        }

        // Same type comparisons
        if (left is long leftLong && right is long rightLong)
        {
            return leftLong.CompareTo(rightLong);
        }

        if (left is string leftStr && right is string rightStr)
        {
            return CompareQualifier(leftStr, rightStr);
        }

        if (left is List<object> leftList && right is List<object> rightList)
        {
            return CompareItems(leftList, rightList);
        }

        // Different type comparisons: string < list < int
        // string vs int → string < int → -1
        if (left is string && right is long)
        {
            return -1;
        }

        if (left is long && right is string)
        {
            return 1;
        }

        // string vs list → string < list → -1
        if (left is string && right is List<object>)
        {
            return -1;
        }

        if (left is List<object> && right is string)
        {
            return 1;
        }

        // list vs int → list < int → -1
        if (left is List<object> && right is long)
        {
            return -1;
        }

        if (left is long && right is List<object>)
        {
            return 1;
        }

        return 0;
    }

    private static readonly Dictionary<string, int> QualifierOrder = new(StringComparer.Ordinal)
    {
        { "alpha", 0 },
        { "beta", 1 },
        { "milestone", 2 },
        { "rc", 3 },
        { "cr", 3 },
        { "snapshot", 4 },
        { "", 5 },
        { "sp", 6 },
    };

    private static int CompareQualifier(string q1, string q2)
    {
        bool has1 = QualifierOrder.TryGetValue(q1, out int o1);
        bool has2 = QualifierOrder.TryGetValue(q2, out int o2);

        if (has1 && has2)
        {
            return o1.CompareTo(o2);
        }

        if (has1)
        {
            // known qualifier vs unknown: unknown sorts after "sp" (6)
            return o1.CompareTo(7);
        }

        if (has2)
        {
            return 7.CompareTo(o2);
        }

        // Both unknown: lexicographic
        return string.Compare(q1, q2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses a lowercased version string into a list of items.
    /// Items are Long (integer segments), String (qualifier segments),
    /// or List&lt;object&gt; (sub-lists created by '-' separators).
    /// </summary>
    private static List<object> ParseVersion(string version)
    {
        var result = new List<object>();
        var current = result;
        var stack = new Stack<List<object>>();

        int i = 0;
        int len = version.Length;

        while (i < len)
        {
            char c = version[i];
            if (c == '.')
            {
                // Dot separator: stays at current level
                i++;
            }
            else if (c == '-')
            {
                // Dash separator: creates a sub-list
                var sub = new List<object>();
                current.Add(sub);
                stack.Push(current);
                current = sub;
                i++;
            }
            else if (char.IsDigit(c))
            {
                int start = i;
                while (i < len && char.IsDigit(version[i]))
                {
                    i++;
                }

                var numStr = version.Substring(start, i - start);
                long.TryParse(
                    numStr,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long num
                );
                current.Add(num);

                // Transition from digits to letters: implicit dash
                if (i < len && char.IsLetter(version[i]))
                {
                    var sub = new List<object>();
                    current.Add(sub);
                    stack.Push(current);
                    current = sub;
                }
            }
            else if (char.IsLetter(c))
            {
                int start = i;
                while (i < len && char.IsLetter(version[i]))
                {
                    i++;
                }

                var qualifier = version.Substring(start, i - start);

                // Check if followed by a digit (letter-to-digit transition)
                bool followedByDigit = i < len && char.IsDigit(version[i]);

                // Normalize single-char qualifiers only when followed by digits
                qualifier = NormalizeQualifier(qualifier, followedByDigit);
                current.Add(qualifier);

                // Transition from letters to digits: implicit dash
                if (followedByDigit)
                {
                    var sub = new List<object>();
                    current.Add(sub);
                    stack.Push(current);
                    current = sub;
                }
            }
            else
            {
                i++; // skip unknown chars
            }
        }

        // Trim trailing null-equivalent items from all levels
        TrimPadding(result);

        return result;
    }

    private static string NormalizeQualifier(string q, bool followedByDigit)
    {
        // Single-char shorthand only expanded at letter-to-digit transitions
        if (followedByDigit && q.Length == 1)
        {
            switch (q)
            {
                case "a":
                    return "alpha";
                case "b":
                    return "beta";
                case "m":
                    return "milestone";
            }
        }

        // Always apply aliases
        switch (q)
        {
            case "ga":
                return "";
            case "final":
                return "";
            case "release":
                return "";
            case "cr":
                return "rc";
            default:
                return q;
        }
    }

    private static void TrimPadding(List<object> items)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item is List<object> subList)
            {
                TrimPadding(subList);
                if (subList.Count == 0)
                {
                    items.RemoveAt(i);
                    continue;
                }
                // Non-empty sub-list is kept, but continue trimming zeros before it
                continue;
            }
            else if (item is long l && l == 0)
            {
                items.RemoveAt(i);
                continue;
            }
            else if (item is string s && s == "")
            {
                items.RemoveAt(i);
                continue;
            }
            break;
        }
    }
}
