# Vers.NET

A .NET Standard 2.0 library for the [vers](https://github.com/package-url/vers-spec) version range specification. Parses, validates, evaluates, and converts version ranges across package ecosystems.

Every package manager invented its own range syntax. npm uses `^1.2.3`. Maven uses `[1.0, 2.0)`. PyPI uses `>=1.0,<2.0`. RubyGems uses `~> 2.2`. Vers is one notation that covers all of them: `vers:npm/>=1.0.0|<3.0.0`.

## Quick start

```csharp
var range = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");

range.Contains("2.5.0");   // true
range.Contains("3.0.0");   // false
range.Contains("0.9.0");   // false
```

Wildcards, exact versions, and disjoint ranges:

```csharp
VersRange.Parse("vers:deb/*").Contains("anything");           // true
VersRange.Parse("vers:pypi/1.0|2.0|3.0").Contains("2.0");    // true
VersRange.Parse("vers:npm/>=1.0.0|<=1.5.0|>=3.0.0|<=4.0.0")
    .Contains("2.0.0");                                       // false (in the gap)
```

## Converting native ranges

Convert ecosystem-specific range syntax to vers and back:

```csharp
// npm's caret notation → vers
var range = NativeRangeConverterRegistry.FromNative("npm", "^1.2.3");
range.ToString(); // "vers:npm/>=1.2.3|<2.0.0"

// NuGet's interval notation → vers
var nuget = NativeRangeConverterRegistry.FromNative("nuget", "[1.0.0, 2.0.0)");
nuget.ToString(); // "vers:nuget/>=1.0.0|<2.0.0"

// RubyGems pessimistic constraint → vers
var gem = NativeRangeConverterRegistry.FromNative("gem", "~>2.0.8");
gem.ToString(); // "vers:gem/>=2.0.8|<2.1"
```

Built-in converters for npm (including `||`, `~`, `^`, hyphen, wildcard), PyPI, NuGet, RubyGems, Conan, OpenSSL, and nginx.

## Building ranges programmatically

```csharp
var range = new VersRange.Builder("npm")
    .AddConstraint(Comparator.GreaterThanOrEqual, "1.0.0")
    .AddConstraint(Comparator.LessThan, "3.0.0")
    .Build();

range.ToString(); // "vers:npm/>=1.0.0|<3.0.0"
```

## Supported versioning schemes

Comparers for every ecosystem in the vers spec:

| Scheme | Aliases | Notes |
| -------- | --------- | ------- |
| semver | npm, golang, cargo, composer | SemVer 2.0.0 with pre-release |
| maven | | Full ComparableVersion algorithm |
| nuget | | Case-insensitive pre-release, 4-part versions |
| deb | | Debian Policy §5.6.12 (epochs, tilde sorting) |
| rpm | | rpmvercmp with epoch and tilde/caret |
| pypi | | PEP 440 (pre/post/dev releases, epochs, local) |
| gem | | RubyGems (string segments = pre-release) |
| gentoo | ebuild, alpine, apk | Gentoo vercmp (suffixes, letter, revision) |
| alpm | arch | Arch Linux (rpmvercmp-style with epoch + release) |
| conan | | Build metadata compared, unlike semver |
| openssl | | Legacy letter suffixes + modern semver |
| cpan | | Perl decimal and dotted-decimal formats |
| intdot | nginx | Dot-separated integers |
| generic | | Fallback for unknown schemes |
| lexicographic | | Bytewise UTF-8 comparison |
| datetime | | RFC 3339 timestamps |
| none | | Empty set (`vers:none/*` only) |
| all | | Universal set (`vers:all/*` only) |

Unknown schemes fall back to the generic comparer.

## Custom schemes

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

After registration, `VersRange.Parse("vers:myscheme/>=1.0")` uses your comparer.

## Validation and simplification

```csharp
var range = VersRange.Parse("vers:npm/>=1.0.0|<3.0.0");
range.Validate();   // throws VersException if invalid
range.Simplify();   // removes redundant constraints per the spec
```

## What's not included

`Invert()`, `Split()`, `Merge()`, and `OverlapsWith()` from the Java/Python implementations. Pull requests welcome.

## Conformance

Tests against the [official vers-spec test data](https://github.com/package-url/vers-spec/tree/main/tests):

Run with `dotnet test`.

## Install

Targets .NET Standard 2.0 (.NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+). No external dependencies.

```sh
dotnet add package Vers
```

## License

MIT
