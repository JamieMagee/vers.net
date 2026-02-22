using System;
using System.Text;

namespace Vers.Schemes;

/// <summary>
/// Compares versions as UTF-8 byte sequences using unsigned bytewise comparison.
/// </summary>
public sealed class LexicographicVersionComparer : IVersionComparer
{
    public static readonly LexicographicVersionComparer Instance =
        new LexicographicVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
            return 0;

        var b1 = Encoding.UTF8.GetBytes(version1);
        var b2 = Encoding.UTF8.GetBytes(version2);

        int len = Math.Min(b1.Length, b2.Length);
        for (int i = 0; i < len; i++)
        {
            int cmp = b1[i].CompareTo(b2[i]);
            if (cmp != 0)
                return cmp;
        }

        return b1.Length.CompareTo(b2.Length);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);
}
