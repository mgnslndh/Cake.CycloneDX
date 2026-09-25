# CdxRefine: Adopt, Exclude and Remove Orphaned Components

Date: 2026-09-25
Status: Draft, awaiting review

## Goal

Extend `CdxRefine` so a build script can shape the component set and dependency graph of an existing
SBOM, typically one produced by `CdxDotNet` and merged with `CdxCliMerge`:

1. **Adopt** orphaned components into the dependency tree (repair a broken tree).
2. **Exclude** components matching name, purl or bom-ref patterns.
3. **Remove orphaned components** left unreachable after exclusion, or left dangling by earlier tools.

Today `CdxRefine` can only assign groups and types, and only `CdxDotNet`'s generation-time
`--exclude-filter` can remove components.

## Model and terminology

An SBOM is either a **tree** or **flat**. A mix of the two is treated as broken.

- **Edge**: a `<dependency ref="X">` nested inside `<dependency ref="P">` means P depends on X.
  Top-level `<dependency>` entries are declarations, not edges.
- **Meta**: the metadata component, `/bom/metadata/component`.
- **Flat SBOM**: `/bom/dependencies` is missing or has no `<dependency>` children.
- **Tree**: a dependency graph exists, and meta has a bom-ref and a top-level `<dependency>` entry.
  All other components are expected to hang below meta.
- **Broken tree**: a dependency graph exists, but meta has no bom-ref or no top-level
  `<dependency>` entry. `cyclonedx-cli merge` in flat mode produces this: the original project
  components become top-level components that nothing depends on, and meta has no bom-ref.
  Whether meta has outgoing edges is deliberately not checked. Exclusions can legitimately remove
  all of meta's children, and orphan removal should then remove everything below them.
- **Orphan**: a top-level component, other than meta, that has a bom-ref and no incoming edge.
- **Unreachable**: a top-level component that cannot be reached from meta by following edges.
  Repeatedly removing orphans until none remain is equivalent to removing everything
  unreachable, except that reachability also catches detached cycles (X ⇄ Y). Orphan removal is
  therefore implemented as reachability.

Nested components (`<component><components><component>`) belong to their parent. They are
removed with their parent, and orphan adoption and orphan removal never evaluate them on their
own. Exclusion does match them (see below).

## Public API

```csharp
var refineSettings = new CdxRefineSettings()
    // 1. Adopt
    .WithAdoptionByName(@"^Foo\.Plugin", parent: new NameCriteria("^Foo$"))
    .WithAdoptOrphanedComponents()                 // remaining orphans go to meta
    // 2. Exclude
    .WithExcludeByName(@"^xunit(\..+)?$")
    .WithExcludeByPurl(@"^pkg:nuget/Microsoft\.CodeAnalysis\.")
    .WithExcludeByBomRef(@"^pkg:nuget/Cake\.Testing@")
    // 3. Remove orphans
    .WithRemoveOrphanedComponents()
    // 4. Existing rules
    .WithGroupByName("Microsoft", "^Microsoft")
    .WithTypeByBomRef("device", "^pkg:nuget/Cake.Core@");

context.CdxRefine(inputFile, outputFile, refineSettings);
```

### `CdxRefineSettings`

```csharp
public class CdxRefineSettings
{
    public List<CdxRefineAdoptionSettings> Adoptions { get; set; } = new();
    public bool AdoptOrphanedComponents { get; set; }
    public List<ICdxComponentCriteria> Exclusions { get; set; } = new();
    public bool RemoveOrphanedComponents { get; set; }
    public List<CdxRefineGroupSettings> GroupSettings { get; set; } = new();   // existing
    public List<CdxRefineTypeSettings> TypeSettings { get; set; } = new();     // existing
}

// Parent == null means meta.
public record CdxRefineAdoptionSettings(ICdxComponentCriteria Criteria, ICdxComponentCriteria? Parent = null);
```

### `CdxRefineSettingsExtensions` additions

| Method | Effect |
|--------|--------|
| `WithAdoptionByName(string pattern, ICdxComponentCriteria? parent = null)` | Adds an adoption rule |
| `WithAdoptionByPurl(string pattern, ICdxComponentCriteria? parent = null)` | Adds an adoption rule |
| `WithAdoptionByBomRef(string pattern, ICdxComponentCriteria? parent = null)` | Adds an adoption rule |
| `WithAdoptOrphanedComponents()` | Sets `AdoptOrphanedComponents = true` |
| `WithExcludeByName(string pattern)` | Adds `NameCriteria` to `Exclusions` |
| `WithExcludeByPurl(string pattern)` | Adds `PurlCriteria` to `Exclusions` |
| `WithExcludeByBomRef(string pattern)` | Adds `BomRefCriteria` to `Exclusions` |
| `WithRemoveOrphanedComponents()` | Sets `RemoveOrphanedComponents = true` |

Patterns are regular expressions, matched case-insensitively (same as the existing criteria).
Custom criteria can be added directly to `Adoptions` and `Exclusions`.

Also fixed: `WithTypeByPurl`'s parameter is renamed from `bomRefPattern` to `purlPattern`.
Callers passing it positionally are unaffected.

## Execution order

`CdxRefine` runs these steps in order. A step is skipped when it has no settings.

1. **Adopt**: repair the tree, so later steps see the real structure.
2. **Exclude**: cut.
3. **Remove orphaned components**: clean up what the cut left behind.
4. **Group**, then **Type**: existing behavior, applied only to what is left.

Adoption runs before exclusion on purpose. Components whose only parent is excluded become orphans
*after* adoption, so orphan adoption cannot anchor them, and orphan removal deletes them.

## Step 1: Adopt

Only orphans can be adopted. Adoption repairs a broken tree; it never rewires existing edges.
Moving a dependency from one parent to another is out of scope (possible future feature).

1. If the SBOM is flat, log at Information level
   (`SBOM has no dependency graph; skipping adoption.`) and skip the step.
2. Compute the orphan set once, before adding any edges.
3. Apply the explicit rules in the order they were added. For each rule:
   - Resolve the parent: meta when `Parent` is null. Otherwise the top-level component matched by
     `Parent`, which must match exactly one component; zero or several matches throw a
     `CakeException` that names the rule's parent criteria and the match count. A resolved
     non-meta parent without a bom-ref throws a `CakeException`.
   - Each orphan matched by `Criteria` that is not yet adopted is adopted by the parent, so the first
     matching rule wins.
   - A matched component that is not an orphan is skipped, with a Verbose log line. This is not
     an error, so broad patterns are safe.
   - A matched orphan that *is* the resolved parent is skipped (Verbose log).
4. If `AdoptOrphanedComponents` is true, meta adopts every orphan that is still not adopted.

**Adopting O into P** means:
- **Cycle check:** if P can be reached from O by following edges, throw a `CakeException` naming
  O and P. The new edge would close a cycle and detach O's subtree from meta.
- Make sure P has a top-level `<dependency ref="P">` entry (create it if missing), then add
  `<dependency ref="O" />` inside it. An edge that already exists is not added again.
- Log at Verbose level: `Adopting component '{O name}' ({O bom-ref}) into '{P name}' ({P bom-ref})`.

**Meta without a bom-ref:** before meta adopts anything, it gets the bom-ref `{name}@{version}`
(or `{name}` when there is no version), matching cyclonedx-cli's hierarchical merge convention. If
another component already uses that bom-ref, throw a `CakeException`.

**Components without a bom-ref** cannot be referenced, so they are never adopted. They remain
unreferenced, and step 3 removes them if it is enabled.

## Step 2: Exclude

1. Find every `<component>` at any depth under `/bom/components` that matches at least one exclusion
   criterion. Components nested inside an already-matched component are removed with it.
2. If meta matches, log a Warning (`Metadata component '{name}' matches an exclusion rule but
   cannot be excluded.`) and keep it.
3. Remove the matched components. Collect the bom-refs of the removed components and of every
   component nested inside them.
4. Remove the dependencies of the removed bom-refs (see below).
5. Log each removed component at Verbose level (`Excluding component '{name}' ({bom-ref})`), and
   log one Information-level summary (`Excluded {n} components.`). If nothing matched, log nothing.

## Step 3: Remove orphaned components

1. **Flat SBOM**: log at Information level
   (`SBOM has no dependency graph; skipping orphan removal.`) and skip the step.
2. **Broken tree**: throw a `CakeException`:
   `Cannot remove orphaned components: the metadata component is not the root of the dependency
   graph. Use WithAdoptOrphanedComponents() or WithAdoptionBy*() to anchor the tree first.`
3. **Tree**: from meta's bom-ref, walk edges to find every reachable bom-ref. Remove every
   top-level component that is not reachable, including components without a bom-ref.
4. Remove the dependencies of the removed bom-refs (see below).
5. Logging mirrors cyclonedx-dotnet (`ExcludeFilterHelper.RemoveOrphanedPackages`): one Warning
   line `The following orphaned components have been removed:` followed by one line per component,
   `  - {name}@{version}`. If nothing was removed, log `No orphaned components were found.` at
   Verbose level.
6. Nested components are never pruned by this step (only top-level components are evaluated). If,
   after the reachability walk and any top-level removals, one or more nested components have a
   bom-ref that is not reachable from meta, log at Information level:
   `{n} nested components are unreachable from the metadata component but were kept; orphan
   removal only evaluates top-level components.`

The reachability walk finds all unreachable components at once, so a single pass is enough.
Removing unreachable components cannot make a reachable component unreachable.

## Removing dependencies of removed bom-refs

Shared by steps 2 and 3. For every removed bom-ref R, remove every `<dependency ref="R">` element
anywhere under `/bom/dependencies`. That covers both R's own top-level entry (with its children)
and every edge pointing to R.

Other places that reference bom-refs (`compositions`, `vulnerabilities/affects`, `services`) are
left alone for now.

## Code structure

`CdxRefineAliases.cs` stays the entry point and runs the steps in order. New internal types keep
each unit small and testable:

| File | Responsibility |
|------|----------------|
| `CdxDependencyGraph.cs` (internal) | Wraps the `XDocument`: flat / tree / broken detection, orphan set, reachability, adding an edge (with entry creation and duplicate check), removing refs |
| `ComponentAdopter.cs` (internal static) | Step 1 |
| `ComponentExcluder.cs` (internal static) | Step 2 |
| `OrphanedComponentRemover.cs` (internal static) | Step 3 |
| `CdxRefineAdoptionSettings.cs` | Public settings record |

The existing group/type code in `CdxRefineAliases` stays as it is. All new code uses braces on
every block (SA1503) and must pass `.\build.ps1 --target test`.

## Testing

xUnit tests in `CdxRefineAliasesTests.cs` (or split per step into sibling files), written test-first,
using XML fixtures in the style of the existing tests. Every output is checked with
`AssertXml.IsValidSbom`.

**Exclude**
- Excludes by name, purl and bom-ref; matching nothing leaves the SBOM unchanged.
- Excluding a parent removes its nested components.
- Nested components are matched and removed on their own.
- Removes the excluded component's top-level dependency entry and all edges pointing to it.
- The metadata component is kept and a warning is logged.
- Excluded components are not touched by group/type rules.

**Remove orphaned components**
- Tree: an exclusion-induced orphan chain (meta → A → X → Y, exclude X) removes Y, including its
  dependency entry.
- Tree: pre-existing unreferenced components and a detached cycle (X ⇄ Y) are removed.
- A component shared with a kept parent is kept.
- Excluding all of meta's children leaves meta with an empty entry and removes everything else.
- Tree: a top-level component without a bom-ref is removed.
- Flat SBOM: no-op.
- Broken tree (flat-merge shape): throws `CakeException`.

**Adopt**
- `WithAdoptOrphanedComponents` on the flat-merge shape: meta gets bom-ref `name@version` and
  edges to both project components; orphan removal then succeeds.
- Explicit rule with the default parent (meta) and with an explicit parent.
- First matching rule wins; later rules and orphan adoption skip already adopted orphans.
- Non-orphans matched by a rule are skipped.
- A rule matching its own parent skips it.
- A parent criteria matching zero or several components throws; a parent without a bom-ref throws.
- Adopting O into its own descendant P throws (cycle).
- A generated meta bom-ref that collides with an existing one throws.
- Existing edges are not duplicated.
- Flat SBOM: no-op.

**Integration**
- The full pipeline (adopt orphans → exclude → remove orphans) on the flat-merge shape produces a
  valid tree without the excluded subtree.

## Out of scope

- Moving a dependency from one parent to another.
- Re-linking an orphan's children to its former parent.
- Cleaning up bom-ref references outside `/bom/dependencies`.
- JSON SBOMs (`CdxRefine` is XML-only today).
