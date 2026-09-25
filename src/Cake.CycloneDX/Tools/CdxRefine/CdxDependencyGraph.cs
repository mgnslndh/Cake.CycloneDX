using System.Xml.Linq;

namespace Cake.CycloneDX.Tools.CdxRefine;

internal sealed class CdxDependencyGraph
{
    private readonly XDocument _document;
    private readonly XNamespace _ns;

    public CdxDependencyGraph(XDocument document, XNamespace ns)
    {
        _document = document;
        _ns = ns;
    }

    public XNamespace Namespace => _ns;

    public XElement? Metadata => _document.Root?.Element(_ns + "metadata")?.Element(_ns + "component");

    public IEnumerable<XElement> TopLevelComponents =>
        _document.Root?.Element(_ns + "components")?.Elements(_ns + "component") ?? Enumerable.Empty<XElement>();

    public IEnumerable<XElement> AllComponents =>
        _document.Root?.Element(_ns + "components")?.Descendants(_ns + "component") ?? Enumerable.Empty<XElement>();

    private XElement? Dependencies => _document.Root?.Element(_ns + "dependencies");

    public static string? GetBomRef(XElement component)
    {
        var value = component.Attribute("bom-ref")?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string GetName(XElement component)
    {
        return component.Element(_ns + "name")?.Value ?? "<unnamed>";
    }

    public ISet<string> GetBomRefsIncludingNested(XElement component)
    {
        var bomRefs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in component.DescendantsAndSelf(_ns + "component"))
        {
            var bomRef = GetBomRef(element);
            if (bomRef != null)
            {
                bomRefs.Add(bomRef);
            }
        }

        return bomRefs;
    }

    public void RemoveReferences(ISet<string> bomRefs)
    {
        var dependencies = Dependencies;
        if (dependencies == null || bomRefs.Count == 0)
        {
            return;
        }

        var matches = dependencies.Descendants(_ns + "dependency")
            .Where(dependency => bomRefs.Contains(dependency.Attribute("ref")?.Value ?? string.Empty))
            .ToList();

        foreach (var dependency in matches)
        {
            dependency.Remove();
        }
    }
}
