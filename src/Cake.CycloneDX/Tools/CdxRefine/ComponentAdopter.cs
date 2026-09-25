using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;

namespace Cake.CycloneDX.Tools.CdxRefine;

internal static class ComponentAdopter
{
    public static void Adopt(ICakeContext context, CdxDependencyGraph graph, IReadOnlyList<CdxRefineAdoptionSettings> rules, bool adoptOrphanedComponents)
    {
        if (graph.IsFlat)
        {
            context.Log.Information("SBOM has no dependency graph; skipping adoption.");
            return;
        }

        var orphans = graph.GetOrphans();
        var orphanSet = new HashSet<XElement>(orphans);
        var adopted = new HashSet<XElement>();

        foreach (var rule in rules)
        {
            var candidates = new List<XElement>();
            foreach (var component in graph.TopLevelComponents.Where(rule.Criteria.IsMatch))
            {
                if (adopted.Contains(component))
                {
                    continue;
                }

                if (!orphanSet.Contains(component))
                {
                    context.Log.Verbose("Skipping adoption of component '{0}': it is not an orphan.", graph.GetName(component));
                    continue;
                }

                candidates.Add(component);
            }

            if (candidates.Count == 0)
            {
                continue;
            }

            var parent = ResolveParent(graph, rule.Parent);
            foreach (var orphan in candidates)
            {
                if (ReferenceEquals(orphan, parent))
                {
                    context.Log.Verbose("Skipping adoption of component '{0}': it is the adoption parent.", graph.GetName(orphan));
                    continue;
                }

                AdoptInto(context, graph, orphan, parent);
                adopted.Add(orphan);
            }
        }

        if (!adoptOrphanedComponents)
        {
            return;
        }

        var remaining = orphans.Where(orphan => !adopted.Contains(orphan)).ToList();
        if (remaining.Count == 0)
        {
            return;
        }

        var metadata = ResolveMetadata(graph);
        foreach (var orphan in remaining)
        {
            AdoptInto(context, graph, orphan, metadata);
        }
    }

    private static XElement ResolveParent(CdxDependencyGraph graph, ICdxComponentCriteria? criteria)
    {
        if (criteria == null)
        {
            return ResolveMetadata(graph);
        }

        var matches = graph.TopLevelComponents.Where(criteria.IsMatch).ToList();
        if (matches.Count != 1)
        {
            throw new CakeException($"Adoption parent criteria ({criteria}) must match exactly one component, but matched {matches.Count}.");
        }

        var parent = matches[0];
        if (CdxDependencyGraph.GetBomRef(parent) == null)
        {
            throw new CakeException($"Adoption parent '{graph.GetName(parent)}' has no bom-ref.");
        }

        return parent;
    }

    private static XElement ResolveMetadata(CdxDependencyGraph graph)
    {
        var metadata = graph.Metadata
            ?? throw new CakeException("Cannot adopt into the metadata component: the SBOM has no metadata component.");

        if (CdxDependencyGraph.GetBomRef(metadata) != null)
        {
            return metadata;
        }

        var name = metadata.Element(graph.Namespace + "name")?.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CakeException("Cannot assign a bom-ref to the metadata component: it has no name.");
        }

        var version = metadata.Element(graph.Namespace + "version")?.Value;
        var bomRef = string.IsNullOrWhiteSpace(version) ? name : $"{name}@{version}";

        if (graph.ContainsBomRef(bomRef))
        {
            throw new CakeException($"Cannot assign bom-ref '{bomRef}' to the metadata component: another component already uses it.");
        }

        metadata.SetAttributeValue("bom-ref", bomRef);
        return metadata;
    }

    private static void AdoptInto(ICakeContext context, CdxDependencyGraph graph, XElement orphan, XElement parent)
    {
        var orphanBomRef = CdxDependencyGraph.GetBomRef(orphan)
            ?? throw new InvalidOperationException("An orphan always has a bom-ref.");
        var parentBomRef = CdxDependencyGraph.GetBomRef(parent)
            ?? throw new InvalidOperationException("An adoption parent always has a bom-ref.");

        if (graph.IsReachable(orphanBomRef, parentBomRef))
        {
            throw new CakeException($"Cannot adopt component '{orphanBomRef}' into '{parentBomRef}': '{parentBomRef}' is a dependency of '{orphanBomRef}', so the adoption would create a cycle.");
        }

        if (graph.AddEdge(parentBomRef, orphanBomRef))
        {
            context.Log.Verbose("Adopting component '{0}' ({1}) into '{2}' ({3})", graph.GetName(orphan), orphanBomRef, graph.GetName(parent), parentBomRef);
        }
    }
}
