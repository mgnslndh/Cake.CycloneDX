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

    public bool IsFlat => !(Dependencies?.Elements(_ns + "dependency").Any() ?? false);

    public string? MetadataBomRef => Metadata is { } metadata ? GetBomRef(metadata) : null;

    public bool IsAnchored
    {
        get
        {
            var metadataBomRef = MetadataBomRef;
            return metadataBomRef != null && GetEntries(metadataBomRef).Any();
        }
    }

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

    public string GetDisplayName(XElement component)
    {
        var version = component.Element(_ns + "version")?.Value;
        return string.IsNullOrWhiteSpace(version) ? GetName(component) : $"{GetName(component)}@{version}";
    }

    public ISet<string> GetReachable(string rootBomRef)
    {
        var childrenByRef = BuildAdjacency();
        var reachable = new HashSet<string>(StringComparer.Ordinal) { rootBomRef };
        var queue = new Queue<string>();
        queue.Enqueue(rootBomRef);

        while (queue.Count > 0)
        {
            if (!childrenByRef.TryGetValue(queue.Dequeue(), out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (reachable.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return reachable;
    }

    public List<XElement> GetOrphans()
    {
        var edges = Dependencies?.Elements(_ns + "dependency").Elements(_ns + "dependency") ?? Enumerable.Empty<XElement>();
        var referenced = new HashSet<string>(
            edges.Select(edge => edge.Attribute("ref")?.Value).OfType<string>(),
            StringComparer.Ordinal);

        return TopLevelComponents
            .Where(component => GetBomRef(component) is { } bomRef && !referenced.Contains(bomRef))
            .ToList();
    }

    public bool IsReachable(string fromBomRef, string toBomRef)
    {
        return GetReachable(fromBomRef).Contains(toBomRef);
    }

    public bool ContainsBomRef(string bomRef)
    {
        return AllComponents.Any(component => GetBomRef(component) == bomRef);
    }

    public bool AddEdge(string parentBomRef, string childBomRef)
    {
        var dependencies = Dependencies ?? throw new InvalidOperationException("SBOM has no dependencies element.");

        if (GetChildren(parentBomRef).Contains(childBomRef, StringComparer.Ordinal))
        {
            return false;
        }

        var entry = GetEntries(parentBomRef).FirstOrDefault();
        if (entry == null)
        {
            entry = new XElement(_ns + "dependency", new XAttribute("ref", parentBomRef));
            dependencies.Add(entry);
        }

        entry.Add(new XElement(_ns + "dependency", new XAttribute("ref", childBomRef)));
        return true;
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

    private IEnumerable<XElement> GetEntries(string bomRef)
    {
        return Dependencies?.Elements(_ns + "dependency").Where(entry => entry.Attribute("ref")?.Value == bomRef)
            ?? Enumerable.Empty<XElement>();
    }

    private IEnumerable<string> GetChildren(string bomRef)
    {
        return GetEntries(bomRef)
            .Elements(_ns + "dependency")
            .Select(edge => edge.Attribute("ref")?.Value)
            .OfType<string>();
    }

    private Dictionary<string, List<string>> BuildAdjacency()
    {
        var childrenByRef = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var entries = Dependencies?.Elements(_ns + "dependency") ?? Enumerable.Empty<XElement>();

        foreach (var entry in entries)
        {
            var parentRef = entry.Attribute("ref")?.Value;
            if (parentRef == null)
            {
                continue;
            }

            if (!childrenByRef.TryGetValue(parentRef, out var children))
            {
                children = new List<string>();
                childrenByRef[parentRef] = children;
            }

            foreach (var edge in entry.Elements(_ns + "dependency"))
            {
                var childRef = edge.Attribute("ref")?.Value;
                if (childRef != null)
                {
                    children.Add(childRef);
                }
            }
        }

        return childrenByRef;
    }
}
