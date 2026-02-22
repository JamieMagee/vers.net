using System.Text.Json;

namespace Vers.Conformance;

public class ConformanceTests
{
    private static readonly string TestDataDir = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "TestData"
    );

    public static IEnumerable<(
        int Index,
        string Vers,
        string Version,
        bool Expected
    )> ContainmentTestData()
    {
        int index = 0;
        foreach (var file in GetTestFiles())
        {
            var tests = LoadTests(file);
            foreach (var test in tests)
            {
                if (test.TestType != "containment")
                    continue;
                if (test.ExpectedFailure)
                    continue;

                var input = test.Input;
                var vers = input.GetProperty("vers").GetString()!;
                var version = input.GetProperty("version").GetString()!;
                var expected = test.ExpectedOutput.GetBoolean();

                yield return (index++, vers, version, expected);
            }
        }
    }

    public static IEnumerable<
        Func<(int Index, string Scheme, string[] InputVersions, string[] ExpectedSorted)>
    > ComparisonTestData()
    {
        int index = 0;
        foreach (var file in GetTestFiles())
        {
            var tests = LoadTests(file);
            foreach (var test in tests)
            {
                if (test.TestType != "comparison")
                    continue;
                if (test.ExpectedFailure)
                    continue;

                var input = test.Input;
                var scheme = input.GetProperty("input_scheme").GetString()!;
                var versions = input
                    .GetProperty("versions")
                    .EnumerateArray()
                    .Select(v => v.GetString()!)
                    .ToArray();
                var expected = test
                    .ExpectedOutput.EnumerateArray()
                    .Select(v => v.GetString()!)
                    .ToArray();

                var idx = index++;
                yield return () => (idx, scheme, versions, expected);
            }
        }
    }

    public static IEnumerable<(
        int Index,
        string Scheme,
        string Version1,
        string Version2,
        bool Expected
    )> EqualityTestData()
    {
        int index = 0;
        foreach (var file in GetTestFiles())
        {
            var tests = LoadTests(file);
            foreach (var test in tests)
            {
                if (test.TestType != "equality")
                    continue;
                if (test.ExpectedFailure)
                    continue;

                var input = test.Input;
                var scheme = input.GetProperty("input_scheme").GetString()!;
                var versions = input
                    .GetProperty("versions")
                    .EnumerateArray()
                    .Select(v => v.GetString()!)
                    .ToArray();
                var expected = test.ExpectedOutput.GetBoolean();

                yield return (index++, scheme, versions[0], versions[1], expected);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(ContainmentTestData))]
    public async Task Containment(int index, string vers, string version, bool expected)
    {
        var range = VersRange.Parse(vers);
        await Assert.That(range.Contains(version)).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(ComparisonTestData))]
    public async Task Comparison(
        int index,
        string scheme,
        string[] inputVersions,
        string[] expectedSorted
    )
    {
        var comparer = VersioningSchemeRegistry.GetComparer(scheme);
        var sorted = inputVersions
            .Select(v => comparer.Normalize(v))
            .OrderBy(v => v, Comparer<string>.Create((a, b) => comparer.Compare(a, b)))
            .ToArray();
        await Assert.That(sorted).IsEquivalentTo(expectedSorted);
    }

    [Test]
    [MethodDataSource(nameof(EqualityTestData))]
    public async Task Equality(
        int index,
        string scheme,
        string version1,
        string version2,
        bool expected
    )
    {
        var comparer = VersioningSchemeRegistry.GetComparer(scheme);
        var result = comparer.Compare(version1, version2) == 0;
        await Assert.That(result).IsEqualTo(expected);
    }

    private static IEnumerable<string> GetTestFiles()
    {
        if (!Directory.Exists(TestDataDir))
            yield break;

        foreach (var file in Directory.GetFiles(TestDataDir, "*.json"))
            yield return file;
    }

    private static List<TestCase> LoadTests(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);

        var result = new List<TestCase>();
        foreach (var element in doc.RootElement.GetProperty("tests").EnumerateArray())
        {
            var tc = new TestCase
            {
                Description = element.GetProperty("description").GetString()!,
                TestGroup = element.GetProperty("test_group").GetString()!,
                TestType = element.GetProperty("test_type").GetString()!,
                Input = element.GetProperty("input"),
                ExpectedFailure =
                    element.TryGetProperty("expected_failure", out var ef) && ef.GetBoolean(),
            };

            if (element.TryGetProperty("expected_output", out var eo))
                tc.ExpectedOutput = eo;

            result.Add(tc);
        }

        return result;
    }

    private class TestCase
    {
        public string Description { get; set; } = "";
        public string TestGroup { get; set; } = "";
        public string TestType { get; set; } = "";
        public JsonElement Input { get; set; }
        public JsonElement ExpectedOutput { get; set; }
        public bool ExpectedFailure { get; set; }
    }
}
