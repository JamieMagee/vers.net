# Vers.NET

A .NET Standard 2.0 library for parsing, validating, and evaluating [vers](https://github.com/package-url/vers-spec) version range specifiers.

`vers` is a URI-based notation for expressing version ranges across different package ecosystems. Instead of learning npm's range syntax *and* Maven's interval notation *and* PyPI's specifiers, you write one format: `vers:npm/>=1.0.0|<3.0.0`.

## Quick start

```csharp
var range = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");

range.Contains("2.5.0");   // true
range.Contains("3.0.0");   // false
range.Contains("0.9.0");   // false
```

Wildcards, exact versions, and disjoint ranges all work:

```csharp
VersRange.Parse("vers:deb/*").Contains("anything");           // true
VersRange.Parse("vers:pypi/1.0|2.0|3.0").Contains("2.0");    // true
VersRange.Parse("vers:npm/>=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0")
    .Contains("2.0.0");                                       // false (in the gap)
```

## Building ranges programmatically

```csharp
var range = new VersRange.Builder("npm")
    .AddConstraint(Comparator.GreaterThanOrEqual, "1.0.0")
    .AddConstraint(Comparator.LessThan, "3.0.0")
    .Build();

// Constraints are sorted automatically.
range.ToString(); // "vers:npm/>=1.0.0|<3.0.0"
```

## Supported versioning schemes

The library ships with comparers for every ecosystem listed in the vers spec:

| Scheme | Aliases | Notes |
| -------- | --------- | ------- |
| semver | npm, golang, cargo, composer | SemVer 2.0.0 with pre-release support |
| maven | | Full ComparableVersion algorithm |
| nuget | | Case-insensitive pre-release, 4-part versions |
| deb | | Debian Policy §5.6.12 (epochs, tilde sorting) |
| rpm | | rpmvercmp with epoch and tilde/caret support |
| pypi | | PEP 440 (pre/post/dev releases, epochs, local) |
| gem | | RubyGems (string segments = pre-release) |
| gentoo | ebuild, alpine, apk | Gentoo vercmp (suffixes, letter, revision) |
| alpm | arch | Arch Linux (rpmvercmp-style with epoch + release) |
| conan | | Build metadata compared, unlike semver |
| openssl | | Legacy letter suffixes + modern semver |
| cpan | | Perl decimal and dotted-decimal formats |
| intdot | nginx | Dot-separated integers (`10.234.5.12`) |
| generic | | Fallback for unknown schemes |
| lexicographic | | Bytewise UTF-8 comparison |
| datetime | | RFC 3339 timestamps |
| none | | Empty set (`vers:none/*` only) |
| all | | Universal set (`vers:all/*` only) |

Unknown schemes fall back to the generic comparer automatically.

## Custom schemes

Register your own comparer for any scheme:

```csharp
public class MyVersionComparer : IVersionComparer
{
    public int Compare(string v1, string v2) { /* ... */ }
    public string Normalize(string v) => v;
    public bool IsValid(string v) => true;
}

VersioningSchemeRegistry.Register(
    new VersioningScheme("myscheme", new MyVersionComparer()));
```

After registration, `VersRange.Parse("vers:myscheme/>=1.0")` will use your comparer.

## Validation and simplification

```csharp
var range = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
range.Validate();   // throws VersException if constraints are invalid
range.Simplify();   // removes redundant constraints per the spec
```

Validation checks: unique versions, sorted order, correct comparator alternation, lone wildcard.

## What's not included (yet)

The spec also defines `FromNative()` conversion (turning `~> 2.2.0` or `>=1.0,<2.0` into a vers string), `Invert()`, `Split()`, and `OverlapsWith()`. Those aren't implemented. Pull requests welcome.

## Conformance

The test suite runs 2,244 tests against the [official vers-spec test data](https://github.com/package-url/vers-spec/tree/main/tests):

- Alpine version comparison (716 tests)
- Maven version comparison (527 tests)
- NuGet version comparison and equality
- Conan version comparison and equality
- Gentoo version comparison and equality
- OpenSSL version comparison and equality
- ALPM/Arch version comparison and equality
- npm and PyPI range containment
- Lexicographic (UTF-8 bytewise) comparison
- RPM, Debian, PyPI, Gem, and CPAN via unit tests from canonical sources

Run everything with `dotnet test`.

## Install

The library targets .NET Standard 2.0, so it works with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+. No external dependencies.

```sh
dotnet add package Vers
```

## License

MIT
