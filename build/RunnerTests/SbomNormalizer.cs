using System.Xml.Linq;

namespace Build.RunnerTests;

/// <summary>
/// Removes run-specific values from an SBOM and sorts it, so SBOMs from different runners can be compared.
/// </summary>
internal static class SbomNormalizer
{
    public static XDocument Normalize(XDocument sbom)
    {
        var copy = new XDocument(sbom);
        var root = copy.Root!;
        var ns = root.GetDefaultNamespace();

        root.Attribute("serialNumber")?.Remove();
        var metadata = root.Element(ns + "metadata");
        metadata?.Element(ns + "timestamp")?.Remove();
        metadata?.Element(ns + "tools")?.Remove();

        SortChildren(root.Element(ns + "components"), element => element.Attribute("bom-ref")?.Value);

        var dependencies = root.Element(ns + "dependencies");
        if (dependencies is not null)
        {
            foreach (var dependency in dependencies.Elements(ns + "dependency"))
            {
                SortChildren(dependency, element => element.Attribute("ref")?.Value);
            }

            SortChildren(dependencies, element => element.Attribute("ref")?.Value);
        }

        return copy;
    }

    /// <returns><see langword="null"/> when equal; otherwise the first differing line of the normalized SBOMs.</returns>
    public static string? FindFirstDifference(XDocument expected, XDocument actual)
    {
        var expectedLines = Normalize(expected).ToString().Split('\n');
        var actualLines = Normalize(actual).ToString().Split('\n');

        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var left = i < expectedLines.Length ? expectedLines[i].Trim() : "<end>";
            var right = i < actualLines.Length ? actualLines[i].Trim() : "<end>";
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                return $"line {i + 1}: '{left}' vs '{right}'";
            }
        }

        return null;
    }

    private static void SortChildren(XElement? parent, Func<XElement, string?> key)
    {
        if (parent is null)
        {
            return;
        }

        var sorted = parent.Elements().OrderBy(element => key(element) ?? string.Empty, StringComparer.Ordinal).ToList();
        parent.RemoveNodes();
        parent.Add(sorted);
    }
}
