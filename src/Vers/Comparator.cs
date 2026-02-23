using System;

namespace Vers;

/// <summary>
/// Comparators used in vers version constraints.
/// </summary>
public enum Comparator
{
    LessThan,
    LessThanOrEqual,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Wildcard,
}

public static class ComparatorExtensions
{
    public static string ToSymbol(this Comparator comparator)
    {
        return comparator switch
        {
            Comparator.LessThan => "<",
            Comparator.LessThanOrEqual => "<=",
            Comparator.Equal => "=",
            Comparator.NotEqual => "!=",
            Comparator.GreaterThan => ">",
            Comparator.GreaterThanOrEqual => ">=",
            Comparator.Wildcard => "*",
            _ => throw new ArgumentOutOfRangeException(nameof(comparator)),
        };
    }

    /// <summary>
    /// Parses a comparator symbol string to its enum value.
    /// Returns null if the string does not match any comparator prefix.
    /// Also returns the number of characters consumed.
    /// </summary>
    public static bool TryParsePrefix(
        string input,
        int startIndex,
        out Comparator comparator,
        out int length
    )
    {
        comparator = Comparator.Equal;
        length = 0;

        if (startIndex >= input.Length)
        {
            return false;
        }

        // Check two-character comparators first
        if (startIndex + 1 < input.Length)
        {
            var two = input.Substring(startIndex, 2);
            switch (two)
            {
                case ">=":
                    comparator = Comparator.GreaterThanOrEqual;
                    length = 2;
                    return true;
                case "<=":
                    comparator = Comparator.LessThanOrEqual;
                    length = 2;
                    return true;
                case "!=":
                    comparator = Comparator.NotEqual;
                    length = 2;
                    return true;
            }
        }

        // Check single-character comparators
        switch (input[startIndex])
        {
            case '<':
                comparator = Comparator.LessThan;
                length = 1;
                return true;
            case '>':
                comparator = Comparator.GreaterThan;
                length = 1;
                return true;
            case '*':
                comparator = Comparator.Wildcard;
                length = 1;
                return true;
        }

        return false;
    }

    public static bool IsGreater(this Comparator c) =>
        c == Comparator.GreaterThan || c == Comparator.GreaterThanOrEqual;

    public static bool IsLesser(this Comparator c) =>
        c == Comparator.LessThan || c == Comparator.LessThanOrEqual;

    public static bool IsInclusive(this Comparator c) =>
        c == Comparator.GreaterThanOrEqual
        || c == Comparator.LessThanOrEqual
        || c == Comparator.Equal;
}
