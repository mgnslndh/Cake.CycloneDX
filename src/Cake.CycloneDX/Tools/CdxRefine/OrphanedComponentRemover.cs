using Cake.Core;
using Cake.Core.Diagnostics;

namespace Cake.CycloneDX.Tools.CdxRefine;

internal static class OrphanedComponentRemover
{
    public static void Remove(ICakeContext context, CdxDependencyGraph graph)
    {
        if (graph.IsFlat)
        {
            context.Log.Information("SBOM has no dependency graph; skipping orphan removal.");
            return;
        }

        var metadataBomRef = graph.MetadataBomRef;
        if (metadataBomRef == null || !graph.IsAnchored)
        {
            throw new CakeException("Cannot remove orphaned components: the metadata component is not the root of the dependency graph. Use WithAdoptOrphanedComponents() or WithAdoptionBy*() to anchor the tree first.");
        }

        var reachable = graph.GetReachable(metadataBomRef);
        var orphans = graph.TopLevelComponents
            .Where(component => CdxDependencyGraph.GetBomRef(component) is not { } bomRef || !reachable.Contains(bomRef))
            .ToList();

        if (orphans.Count == 0)
        {
            context.Log.Verbose("No orphaned components were found.");
            return;
        }

        context.Log.Warning("The following orphaned components have been removed:");
        var removedBomRefs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var orphan in orphans)
        {
            context.Log.Warning("  - {0}", graph.GetDisplayName(orphan));
            removedBomRefs.UnionWith(graph.GetBomRefsIncludingNested(orphan));
            orphan.Remove();
        }

        graph.RemoveReferences(removedBomRefs);
    }
}
