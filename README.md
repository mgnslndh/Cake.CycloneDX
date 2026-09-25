# Cake.CycloneDX

[![NuGet Version](http://img.shields.io/nuget/v/Cake.CycloneDX.svg?style=flat)](https://www.nuget.org/packages/Cake.CycloneDX/) [![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md) [![CI](https://github.com/mgnslndh/Cake.CycloneDX/actions/workflows/main.yml/badge.svg)](https://github.com/mgnslndh/Cake.CycloneDX/actions/workflows/main.yml) ![CodeRabbit Pull Request Reviews](https://img.shields.io/coderabbit/prs/github/mgnslndh/Cake.CycloneDX?logo=red&logoColor=%23FF570A)

Cake addin to run the [CycloneDX .NET](https://github.com/CycloneDX/cyclonedx-dotnet) and [CycloneDX CLI](https://github.com/CycloneDX/cyclonedx-cli) tools.

## CdxRefine

`CdxRefine` shapes an existing XML SBOM's component set and dependency graph, typically one
produced by `CdxDotNet` and merged with `CdxCliMerge`. It runs these steps, in order, skipping
any step that has no settings:

1. **Adopt** orphaned components into the dependency tree (repair a broken tree).
2. **Exclude** components matching a name, purl or bom-ref pattern.
3. **Remove orphaned components** left unreachable after exclusion, or left dangling for other
   reasons.
4. **Group** and **Type**, applied only to what is left.

A `cyclonedx-cli merge` in flat mode produces a *broken tree*: the original project components
become top-level components that nothing depends on, and the metadata component has no bom-ref.
`WithRemoveOrphanedComponents()` throws on a broken tree; adopt the orphans first to anchor it:

```csharp
var settings = new CdxRefineSettings()
    .WithAdoptOrphanedComponents()
    .WithExcludeByName("^xunit(\\..+)?$")
    .WithRemoveOrphanedComponents()
    .WithGroupByName("Test tools", "^xunit");

context.CdxRefine(inputFile, outputFile, settings);
```

Notes:

- A flat SBOM (no `/bom/dependencies` entries) is left alone by adoption and orphan removal;
  both steps just log and skip.
- Only orphans (top-level components with no incoming edge) are ever adopted. A component that
  already has a parent is skipped, not moved.
- An adoption rule's `parent` criteria is only resolved when the rule actually matches a
  component. A rule with a `parent` pattern that matches nothing never throws, because it never
  runs.
- Orphan removal only evaluates top-level components. Nested components
  (`<component><components><component>`) that become unreachable in a hierarchical merge are kept
  with their parent and logged, never pruned on their own.
- In a hierarchical merge, nested package bom-refs are namespaced by their owning project, e.g.
  `Proj@1.0:pkg:nuget/Newtonsoft.Json@13.0.3`. Account for that prefix in `WithAdoptionByBomRef`,
  `WithExcludeByBomRef` and similar bom-ref patterns.
