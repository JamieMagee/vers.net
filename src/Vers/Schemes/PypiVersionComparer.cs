using System;
using System.Globalization;

namespace Vers.Schemes;

/// <summary>
/// Implements PyPI (PEP 440) version comparison.
///
/// Format: [N!]N(.N)*[{a|b|rc}N][.postN][.devN][+local]
///
/// Epoch (N!) is compared first. Release segments compared numerically
/// with implicit zero padding. Pre-release (alpha/beta/rc) sorts before
/// release. Post-release sorts after. Dev sorts before everything at its level.
/// Local version (+local) is compared only when both present.
///
/// Normalization: pre/preview/c → rc, alpha → a, beta → b.
/// </summary>
public sealed class PypiVersionComparer : IVersionComparer
{
    public static readonly PypiVersionComparer Instance = new PypiVersionComparer();

    public int Compare(string version1, string version2)
    {
        if (version1 == version2)
        {
            return 0;
        }

        var v1 = ParsePep440(version1);
        var v2 = ParsePep440(version2);

        return v1.CompareTo(v2);
    }

    public string Normalize(string version) => version;

    public bool IsValid(string version) => !string.IsNullOrEmpty(version);

    // Pre-release kind ordering: dev=-1, a=0, b=1, rc=2, (none)=3, post=4
    private const int KindDev = -1;
    private const int KindAlpha = 0;
    private const int KindBeta = 1;
    private const int KindRc = 2;
    private const int KindFinal = 3;
    private const int KindPost = 4;

    private struct Pep440Version : IComparable<Pep440Version>
    {
        public long Epoch;
        public long[] Release;
        public int PreKind; // KindAlpha/KindBeta/KindRc or KindFinal if none
        public long PreNum;
        public bool HasPost;
        public long PostNum;
        public bool HasDev;
        public long DevNum;
        public string? Local;

        public int CompareTo(Pep440Version other)
        {
            // 1. Epoch
            int cmp = Epoch.CompareTo(other.Epoch);
            if (cmp != 0)
            {
                return cmp;
            }

            // 2. Release segments (zero-padded)
            int maxRel = Math.Max(Release.Length, other.Release.Length);
            for (int i = 0; i < maxRel; i++)
            {
                long a = i < Release.Length ? Release[i] : 0;
                long b = i < other.Release.Length ? other.Release[i] : 0;
                cmp = a.CompareTo(b);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            // 3. Dev before pre before release before post
            // The ordering key is: (dev, pre, final/post)
            // dev: sorts before everything at its level
            // pre + dev: sorts before pre without dev
            // pre: sorts before final
            // final: the release
            // post: sorts after final
            // post + dev: sorts before post without dev

            // Build a comparable tuple: (preKind, preNum, hasPost, postNum, hasDev, devNum)
            // But the actual PEP 440 ordering is more nuanced:
            //   X.YaN.devM < X.YaN < X.YbN < X.YrcN < X.Y.devM < X.Y < X.Y.postN.devM < X.Y.postN

            int phase1 = GetPhase(this);
            int phase2 = GetPhase(other);
            cmp = phase1.CompareTo(phase2);
            if (cmp != 0)
            {
                return cmp;
            }

            // Within same phase, compare details
            if (PreKind != KindFinal || other.PreKind != KindFinal)
            {
                cmp = PreKind.CompareTo(other.PreKind);
                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = PreNum.CompareTo(other.PreNum);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            if (HasPost || other.HasPost)
            {
                cmp = HasPost.CompareTo(other.HasPost);
                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = PostNum.CompareTo(other.PostNum);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            if (HasDev || other.HasDev)
            {
                // Has dev sorts BEFORE no dev
                if (HasDev != other.HasDev)
                {
                    return HasDev ? -1 : 1;
                }

                cmp = DevNum.CompareTo(other.DevNum);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            // 4. Local version: no local < has local, compared segment by segment
            if (Local == null && other.Local == null)
            {
                return 0;
            }

            if (Local == null)
            {
                return -1;
            }

            if (other.Local == null)
            {
                return 1;
            }

            return CompareLocal(Local, other.Local);
        }

        /// <summary>
        /// Assigns a phase number for broad ordering:
        /// dev(no pre)=0, pre+dev=1, pre=2, final=3, post+dev=4, post=5
        /// </summary>
        private static int GetPhase(Pep440Version v)
        {
            if (v.PreKind < KindFinal)
            {
                // Has pre-release
                return v.HasDev ? 1 : 2;
            }

            if (!v.HasPost)
            {
                // No post
                return v.HasDev ? 0 : 3;
            }

            // Has post
            return v.HasDev ? 4 : 5;
        }
    }

    private static int CompareLocal(string a, string b)
    {
        var segs1 = a.Split('.');
        var segs2 = b.Split('.');
        int maxLen = Math.Max(segs1.Length, segs2.Length);

        for (int i = 0; i < maxLen; i++)
        {
            var s1 = i < segs1.Length ? segs1[i] : "";
            var s2 = i < segs2.Length ? segs2[i] : "";

            bool isNum1 = long.TryParse(
                s1,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long n1
            );
            bool isNum2 = long.TryParse(
                s2,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long n2
            );

            int cmp;
            if (isNum1 && isNum2)
            {
                cmp = n1.CompareTo(n2);
            }
            else if (isNum1)
            {
                // numeric > alpha in local
                cmp = 1;
            }
            else if (isNum2)
            {
                cmp = -1;
            }
            else
            {
                cmp = string.Compare(
                    s1.ToLowerInvariant(),
                    s2.ToLowerInvariant(),
                    StringComparison.Ordinal
                );
            }

            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    private static Pep440Version ParsePep440(string version)
    {
        var result = new Pep440Version
        {
            Epoch = 0,
            Release = [0L],
            PreKind = KindFinal,
            PreNum = 0,
            HasPost = false,
            PostNum = 0,
            HasDev = false,
            DevNum = 0,
            Local = null,
        };

        var s = version.Trim().ToLowerInvariant();

        // Strip leading v
        if (s.StartsWith("v"))
        {
            s = s.Substring(1);
        }

        // Extract local
        int plusIdx = s.IndexOf('+');
        if (plusIdx >= 0)
        {
            result.Local = s.Substring(plusIdx + 1);
            s = s.Substring(0, plusIdx);
        }

        // Extract epoch
        int bangIdx = s.IndexOf('!');
        if (bangIdx > 0)
        {
            long.TryParse(
                s.Substring(0, bangIdx),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out result.Epoch
            );
            s = s.Substring(bangIdx + 1);
        }

        // Parse using positional scanning
        int pos = 0;

        // Release segments
        var releaseSegs = new System.Collections.Generic.List<long>();
        while (pos < s.Length)
        {
            if (char.IsDigit(s[pos]))
            {
                int start = pos;
                while (pos < s.Length && char.IsDigit(s[pos]))
                {
                    pos++;
                }

                long.TryParse(
                    s.Substring(start, pos - start),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long seg
                );
                releaseSegs.Add(seg);

                if (pos < s.Length && s[pos] == '.')
                {
                    // Check if next char after dot is a digit (more release segments)
                    // or a letter (suffix like .post, .dev, .a, etc.)
                    if (pos + 1 < s.Length && char.IsDigit(s[pos + 1]))
                    {
                        pos++; // skip dot, continue release
                        continue;
                    }
                    else
                    {
                        break; // dot followed by letter = suffix separator
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        if (releaseSegs.Count > 0)
        {
            result.Release = releaseSegs.ToArray();
        }

        // Skip separator
        if (pos < s.Length && (s[pos] == '.' || s[pos] == '-' || s[pos] == '_'))
        {
            pos++;
        }

        // Pre-release
        if (pos < s.Length)
        {
            int preLen = TryMatchPre(s, pos);
            if (preLen > 0)
            {
                var preStr = s.Substring(pos, preLen);
                result.PreKind = NormalizePre(preStr);
                pos += preLen;

                // Skip separator
                if (pos < s.Length && (s[pos] == '.' || s[pos] == '-' || s[pos] == '_'))
                {
                    pos++;
                }

                // Pre number
                int numStart = pos;
                while (pos < s.Length && char.IsDigit(s[pos]))
                {
                    pos++;
                }

                if (pos > numStart)
                {
                    long.TryParse(
                        s.Substring(numStart, pos - numStart),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out result.PreNum
                    );
                }

                // Skip separator
                if (pos < s.Length && (s[pos] == '.' || s[pos] == '-' || s[pos] == '_'))
                {
                    pos++;
                }
            }
        }

        // Post-release
        if (pos < s.Length)
        {
            int postLen = TryMatchPost(s, pos);
            if (postLen > 0)
            {
                result.HasPost = true;
                pos += postLen;

                if (pos < s.Length && (s[pos] == '.' || s[pos] == '-' || s[pos] == '_'))
                {
                    pos++;
                }

                int numStart = pos;
                while (pos < s.Length && char.IsDigit(s[pos]))
                {
                    pos++;
                }

                if (pos > numStart)
                {
                    long.TryParse(
                        s.Substring(numStart, pos - numStart),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out result.PostNum
                    );
                }

                if (pos < s.Length && (s[pos] == '.' || s[pos] == '-' || s[pos] == '_'))
                {
                    pos++;
                }
            }
        }

        // Dev-release
        if (pos < s.Length)
        {
            if (pos + 3 <= s.Length && s.Substring(pos, 3) == "dev")
            {
                result.HasDev = true;
                pos += 3;

                if (pos < s.Length && (s[pos] == '.' || s[pos] == '-' || s[pos] == '_'))
                {
                    pos++;
                }

                int numStart = pos;
                while (pos < s.Length && char.IsDigit(s[pos]))
                {
                    pos++;
                }

                if (pos > numStart)
                {
                    long.TryParse(
                        s.Substring(numStart, pos - numStart),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out result.DevNum
                    );
                }
            }
        }

        return result;
    }

    private static int TryMatchPre(string s, int pos)
    {
        string[] prefixes = ["alpha", "beta", "preview", "rc", "a", "b", "c"];
        foreach (var p in prefixes)
        {
            if (pos + p.Length <= s.Length && s.Substring(pos, p.Length) == p)
            {
                // Make sure it's not a prefix of "post" or "dev"
                if (p == "a" && pos + 1 < s.Length && s[pos + 1] == 'l')
                {
                    continue; // "alpha" will match instead
                }

                if (p == "b" && pos + 1 < s.Length && s[pos + 1] == 'e')
                {
                    continue; // "beta" will match instead
                }

                return p.Length;
            }
        }
        return 0;
    }

    private static int TryMatchPost(string s, int pos)
    {
        if (pos + 4 <= s.Length && s.Substring(pos, 4) == "post")
        {
            return 4;
        }

        if (pos + 3 <= s.Length && s.Substring(pos, 3) == "rev")
        {
            return 3;
        }

        if (pos + 1 <= s.Length && s[pos] == 'r' && pos + 1 < s.Length && char.IsDigit(s[pos + 1]))
        {
            return 1;
        }

        return 0;
    }

    private static int NormalizePre(string pre)
    {
        return pre switch
        {
            "a" or "alpha" => KindAlpha,
            "b" or "beta" => KindBeta,
            "c" or "rc" or "preview" => KindRc,
            _ => KindFinal,
        };
    }
}
