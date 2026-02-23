using System;
using System.Collections.Generic;
using System.Linq;

namespace Vers.NativeRanges;

/// <summary>
/// Converts OpenSSL native version list notation to vers and back.
/// Format: comma-separated list of bare versions, sorted in vers output.
/// </summary>
public sealed class OpensslNativeRangeConverter : INativeRangeConverter
{
    public static readonly OpensslNativeRangeConverter Instance = new OpensslNativeRangeConverter();

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
            var ver = part.Trim();
            if (!string.IsNullOrEmpty(ver))
            {
                constraints.Add(new VersionConstraint(Comparator.Equal, ver));
            }
        }

        if (constraints.Count == 0)
        {
            throw new VersException("No versions found in native range.");
        }

        var comparer = VersioningSchemeRegistry.GetComparer("openssl");
        return new VersRange.Builder("openssl")
            .AddConstraints(constraints)
            .BuildSortedNoValidation(comparer);
    }

    public string ToNative(VersRange range)
    {
        return string.Join(", ", range.Constraints.Select(c => c.Version));
    }
}
