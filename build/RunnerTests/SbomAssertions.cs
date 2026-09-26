using System.Xml.Linq;

namespace Build.RunnerTests;

/// <summary>
/// Checks a runner's final SBOM against what the scenario pipeline must produce.
/// </summary>
internal static class SbomAssertions
{
    public static IReadOnlyList<string> Check(XDocument sbom)
    {
        var failures = new List<string>();
        var root = sbom.Root!;
        var ns = root.GetDefaultNamespace();

        var metadata = root.Element(ns + "metadata")?.Element(ns + "component");
        Expect(failures, "metadata component group", "com.example", metadata?.Element(ns + "group")?.Value);
        Expect(failures, "metadata component name", "Scenario", metadata?.Element(ns + "name")?.Value);
        Expect(failures, "metadata component version", "1.2.3", metadata?.Element(ns + "version")?.Value);

        var components = root.Element(ns + "components")?.Elements(ns + "component").ToList() ?? new List<XElement>();
        var names = components
            .Select(component => component.Element(ns + "name")?.Value)
            .OrderBy(name => name, StringComparer.Ordinal);
        Expect(failures, "top-level components", "App, Lib, Newtonsoft.Json", string.Join(", ", names));

        var newtonsoft = components.FirstOrDefault(component => component.Element(ns + "name")?.Value == "Newtonsoft.Json");
        Expect(failures, "Newtonsoft.Json group", "JamesNK", newtonsoft?.Element(ns + "group")?.Value);

        var lib = components.FirstOrDefault(component => component.Element(ns + "name")?.Value == "Lib");
        Expect(failures, "Lib type", "framework", lib?.Attribute("type")?.Value);

        var scenarioDependencies = root.Element(ns + "dependencies")?
            .Elements(ns + "dependency")
            .FirstOrDefault(dependency => dependency.Attribute("ref")?.Value == "Scenario@1.2.3")?
            .Elements(ns + "dependency")
            .Select(dependency => dependency.Attribute("ref")?.Value)
            .OrderBy(reference => reference, StringComparer.Ordinal);
        Expect(
            failures,
            "Scenario@1.2.3 dependencies",
            "App@1.0.0, Lib@1.0.0",
            scenarioDependencies is null ? null : string.Join(", ", scenarioDependencies));

        return failures;
    }

    private static void Expect(List<string> failures, string what, string expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            failures.Add($"{what}: expected '{expected}' but was '{actual ?? "<missing>"}'");
        }
    }
}
