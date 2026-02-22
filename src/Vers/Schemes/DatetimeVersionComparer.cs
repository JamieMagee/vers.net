using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Compares versions as RFC3339 timestamps via DateTimeOffset parsing.
/// </summary>
public sealed class DatetimeVersionComparer : IVersionComparer
{
    public static readonly DatetimeVersionComparer Instance = new DatetimeVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
            return 0;

        var dt1 = ParseTimestamp(version1);
        var dt2 = ParseTimestamp(version2);

        return dt1.CompareTo(dt2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version)
    {
        return DateTimeOffset.TryParse(
            version,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        );
    }

    private static DateTimeOffset ParseTimestamp(string version)
    {
        if (
            DateTimeOffset.TryParse(
                version,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result
            )
        )
            return result;

        throw new VersException($"Invalid datetime version: '{version}'. Expected RFC3339 format.");
    }
}
