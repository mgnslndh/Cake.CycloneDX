using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;

namespace Cake.CycloneDX.Tools.CdxRefine;

internal static class ComponentExcluder
{
    public static void Exclude(ICakeContext context, CdxDependencyGraph graph, IReadOnlyCollection<ICdxComponentCriteria> exclusions)
    {
        var metadata = graph.Metadata;
        if (metadata != null && exclusions.Any(criteria => criteria.IsMatch(metadata)))
        {
            context.Log.Warning("Metadata component '{0}' matches an exclusion rule but cannot be excluded.", graph.GetName(metadata));
        }

        var matched = graph.AllComponents
            .Where(component => exclusions.Any(criteria => criteria.IsMatch(component)))
            .ToList();

        var matchedSet = new HashSet<XElement>(matched);
        var outermost = matched
            .Where(component => !component.Ancestors().Any(matchedSet.Contains))
            .ToList();

        if (outermost.Count == 0)
        {
            return;
        }

        var removedBomRefs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in outermost)
        {
            context.Log.Verbose("Excluding component '{0}' ({1})", graph.GetName(component), CdxDependencyGraph.GetBomRef(component) ?? "no bom-ref");
            removedBomRefs.UnionWith(graph.GetBomRefsIncludingNested(component));
            component.Remove();
        }

        graph.RemoveReferences(removedBomRefs);
        context.Log.Information("Excluded {0} components.", outermost.Count);
    }
}
