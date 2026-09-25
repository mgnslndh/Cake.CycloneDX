# CdxRefine Adopt, Exclude and Remove Orphans Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let `CdxRefine` adopt orphaned components into the dependency tree, exclude components by
pattern, and remove orphaned components from an existing CycloneDX XML SBOM.

**Architecture:** An internal `CdxDependencyGraph` wraps the SBOM `XDocument` and answers every
graph question: flat or tree, orphans, reachability, adding edges and removing refs. Each step
(`ComponentAdopter`, `ComponentExcluder`, `OrphanedComponentRemover`) is a small internal static
class that uses the graph. `CdxRefineAliases.CdxRefine(XDocument, ...)` runs them in the fixed
order adopt, then exclude, then remove orphans, before the existing group/type rules.

**Tech Stack:** C# (LangVersion preview), .NET 8/9/10, `System.Xml.Linq`, Cake.Core, xUnit v3,
NSubstitute, Cake.Testing (`FakeLog`), CycloneDX.Core (schema validation in tests), StyleCop.Analyzers.

**Spec:** `docs/superpowers/specs/2026-09-25-cdxrefine-dependency-graph-design.md`

## Global Constraints

- Validate with `.\build.ps1 --target test` (Release, all target frameworks, warnings as errors). `dotnet test` alone does **not** catch StyleCop errors (AGENTS.md).
- SA1503: always write braces for `if`, `else`, `foreach`, `while`, `for` and `using` bodies, even single-line ones.
- Library code (`src/Cake.CycloneDX`): file-scoped namespaces, `Nullable` enabled, `using` directives outside the namespace with `System.*` first, private fields named `_camelCase`, no `this.` prefix, no XML doc comments (match the existing files).
- Test code (`src/Cake.CycloneDX.Tests`): `Nullable` disabled, xUnit `[Fact]`, `Substitute.For<ICakeContext>()`, and every output checked with `AssertXml.IsValidSbom`.
- Patterns are case-insensitive regular expressions via the existing `NameCriteria`, `PurlCriteria` and `BomRefCriteria`. bom-ref comparisons are ordinal (case-sensitive).
- Order of steps: adopt, then exclude, then remove orphans, then group, then type.
- Log messages, copied verbatim from the spec:
  - `SBOM has no dependency graph; skipping adoption.` (Information)
  - `Adopting component '{O name}' ({O bom-ref}) into '{P name}' ({P bom-ref})` (Verbose)
  - `Metadata component '{name}' matches an exclusion rule but cannot be excluded.` (Warning)
  - `Excluding component '{name}' ({bom-ref})` (Verbose)
  - `Excluded {n} components.` (Information)
  - `SBOM has no dependency graph; skipping orphan removal.` (Information)
  - `The following orphaned components have been removed:` then `  - {name}@{version}` (Warning)
  - `No orphaned components were found.` (Verbose)
- Broken-tree exception message, verbatim: `Cannot remove orphaned components: the metadata component is not the root of the dependency graph. Use WithAdoptOrphanedComponents() or WithAdoptionBy*() to anchor the tree first.`
- Existing group/type behavior must not change.
- Commit messages follow the repo's gitmoji style (`:sparkles: feat: ...`) and end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

These are inputs the spec implies but doesn't spell out, most likely to cause problems first. Each
has a test in the owning task.

1. An exclusion matching both a parent and its nested child must remove the parent once, count it once and not throw (Task 1: `ShouldExcludeParentAndMatchingNestedChildOnce`).
2. A merged SBOM can have several top-level `<dependency ref="a">` entries for the same ref. Reachability must follow the edges in all of them (Task 2: `ShouldFollowEdgesAcrossDuplicateDependencyEntries`).
3. In a hierarchical merge, nested components are part of the graph. Nested components of a reachable parent must be kept (Task 2: `ShouldKeepNestedComponentsOfReachableParent`).
4. An SBOM with a dependency graph but no `<metadata>` must produce a `CakeException`, not a `NullReferenceException`, for both orphan removal and adoption into meta (Task 2: `ShouldThrowWhenSbomHasNoMetadataButHasGraph`; Task 3: `ShouldThrowWhenAdoptingIntoMissingMetadata`).
5. An adoption rule with the default parent that matches nothing must leave meta untouched, so no bom-ref is assigned (Task 3: `ShouldNotAssignMetadataBomRefWhenNothingIsAdopted`).

## File Structure

| File | Status | Responsibility |
|------|--------|----------------|
| `src/Cake.CycloneDX/Tools/CdxRefine/CdxDependencyGraph.cs` | Create (Task 1, extended in 2 and 3) | Graph queries and mutations on the SBOM document |
| `src/Cake.CycloneDX/Tools/CdxRefine/ComponentExcluder.cs` | Create (Task 1) | Exclude step |
| `src/Cake.CycloneDX/Tools/CdxRefine/OrphanedComponentRemover.cs` | Create (Task 2) | Remove-orphans step |
| `src/Cake.CycloneDX/Tools/CdxRefine/ComponentAdopter.cs` | Create (Task 3) | Adopt step |
| `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAdoptionSettings.cs` | Create (Task 3) | Public adoption rule record |
| `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettings.cs` | Modify (Tasks 1–3) | New settings properties |
| `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs` | Modify (Tasks 1–4) | New `With…` methods; parameter rename |
| `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAliases.cs` | Modify (Tasks 1–3) | Runs the steps in order |
| `src/Cake.CycloneDX/Tools/CdxRefine/NameCriteria.cs`, `PurlCriteria.cs`, `BomRefCriteria.cs` | Modify (Task 3) | `ToString()` for error messages |
| `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/TestSbom.cs` | Create (Task 1) | SBOM fixture builder and `Refine` helper |
| `src/Cake.CycloneDX.Tests/Assertions/AssertXml.cs` | Modify (Task 1) | Component and dependency assertions |
| `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineExcludeTests.cs` | Create (Task 1) | Exclude tests |
| `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineRemoveOrphansTests.cs` | Create (Task 2) | Remove-orphans tests |
| `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineAdoptTests.cs` | Create (Task 3) | Adopt tests |
| `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefinePipelineTests.cs` | Create (Task 4) | End-to-end test of all steps together |

## CycloneDX XML background (read once)

```xml
<bom xmlns="http://cyclonedx.org/schema/bom/1.6" version="1">
  <metadata>
    <component type="application" bom-ref="app"><name>App</name></component>  <!-- "meta" -->
  </metadata>
  <components>
    <component type="library" bom-ref="a"><name>A</name>
      <components>                                                           <!-- nested -->
        <component type="library" bom-ref="n"><name>N</name></component>
      </components>
    </component>
  </components>
  <dependencies>
    <dependency ref="app">              <!-- top-level entry = declaration for "app" -->
      <dependency ref="a" />            <!-- nested = edge: app depends on a -->
    </dependency>
    <dependency ref="a" />
  </dependencies>
</bom>
```

Element order inside `<component>` is fixed by the schema: `group`, `name`, `version`, …, `purl`,
…, `components`. The fixture builder in Task 1 respects it.

---

### Task 1: Exclude components

**Files:**
- Create: `src/Cake.CycloneDX/Tools/CdxRefine/CdxDependencyGraph.cs`
- Create: `src/Cake.CycloneDX/Tools/CdxRefine/ComponentExcluder.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettings.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAliases.cs:52-77`
- Create: `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/TestSbom.cs`
- Modify: `src/Cake.CycloneDX.Tests/Assertions/AssertXml.cs`
- Test: `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineExcludeTests.cs`

**Interfaces:**
- Consumes: `ICdxComponentCriteria.IsMatch(XElement)`, `NameCriteria`, `PurlCriteria`, `BomRefCriteria` (existing).
- Produces:
  - `CdxRefineSettings.Exclusions : List<ICdxComponentCriteria>`
  - `WithExcludeByName(string namePattern)`, `WithExcludeByPurl(string purlPattern)`, `WithExcludeByBomRef(string bomRefPattern)`, each returning `CdxRefineSettings`
  - `internal sealed class CdxDependencyGraph(XDocument document, XNamespace ns)` with `Namespace`, `Metadata`, `TopLevelComponents`, `AllComponents`, `static string? GetBomRef(XElement)`, `string GetName(XElement)`, `ISet<string> GetBomRefsIncludingNested(XElement)`, `void RemoveReferences(ISet<string>)`
  - `internal static class ComponentExcluder` with `Exclude(ICakeContext, CdxDependencyGraph, IReadOnlyCollection<ICdxComponentCriteria>)`
  - Test helpers: `TestSbom.Create/Metadata/Component/Dependency/Refine/MetadataBomRef`, `AssertXml.HasComponent/DoesNotHaveComponent/HasDependency/DoesNotHaveDependency/IsNotReferencedInDependencies`

- [ ] **Step 1: Add the test fixture builder**

Create `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/TestSbom.cs`:

```csharp
using System.Xml.Linq;
using Cake.Core;
using Cake.CycloneDX.Tools.CdxRefine;
using Cake.Testing;
using NSubstitute;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxRefine;

internal static class TestSbom
{
    public static string Create(string metadata, string components, string dependencies = null)
    {
        var metadataXml = metadata == null ? string.Empty : $"<metadata>{metadata}</metadata>";
        var dependenciesXml = dependencies == null ? string.Empty : $"<dependencies>{dependencies}</dependencies>";
        return "<bom xmlns=\"http://cyclonedx.org/schema/bom/1.6\" serialNumber=\"urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032\" version=\"1\">"
            + metadataXml
            + $"<components>{components}</components>"
            + dependenciesXml
            + "</bom>";
    }

    public static string Metadata(string name, string version = null, string bomRef = null)
    {
        var bomRefXml = bomRef == null ? string.Empty : $" bom-ref=\"{bomRef}\"";
        var versionXml = version == null ? string.Empty : $"<version>{version}</version>";
        return $"<component type=\"application\"{bomRefXml}><name>{name}</name>{versionXml}</component>";
    }

    public static string Component(string bomRef, string name = null, string purl = null, string nested = null, string version = null)
    {
        var bomRefXml = bomRef == null ? string.Empty : $" bom-ref=\"{bomRef}\"";
        var versionXml = version == null ? string.Empty : $"<version>{version}</version>";
        var purlXml = purl == null ? string.Empty : $"<purl>{purl}</purl>";
        var nestedXml = nested == null ? string.Empty : $"<components>{nested}</components>";
        return $"<component type=\"library\"{bomRefXml}><name>{name ?? bomRef}</name>{versionXml}{purlXml}{nestedXml}</component>";
    }

    public static string Dependency(string bomRef, params string[] dependsOn)
    {
        var children = string.Concat(dependsOn.Select(child => $"<dependency ref=\"{child}\" />"));
        return $"<dependency ref=\"{bomRef}\">{children}</dependency>";
    }

    public static string Refine(string xml, CdxRefineSettings settings, FakeLog log = null)
    {
        var context = Substitute.For<ICakeContext>();
        if (log != null)
        {
            context.Log.Returns(log);
        }

        return context.CdxRefine(xml, settings);
    }

    public static string MetadataBomRef(string xml)
    {
        var document = XDocument.Parse(xml);
        XNamespace ns = document.Root.Name.Namespace;
        return (string)document.Root.Element(ns + "metadata")?.Element(ns + "component")?.Attribute("bom-ref");
    }
}
```

- [ ] **Step 2: Add the assertion helpers**

In `src/Cake.CycloneDX.Tests/Assertions/AssertXml.cs`, add these members inside `internal class AssertXml`, after `HaveComponentWithAttribute`:

```csharp
        public static void HasComponent(string xml, string bomRef)
        {
            if (CountComponents(xml, bomRef) == 0)
            {
                throw new XunitException($"Expected component with bom-ref '{bomRef}' but it does not exist.");
            }
        }

        public static void DoesNotHaveComponent(string xml, string bomRef)
        {
            int count = CountComponents(xml, bomRef);
            if (count != 0)
            {
                throw new XunitException($"Expected no component with bom-ref '{bomRef}' but found {count}.");
            }
        }

        public static void HasDependency(string xml, string parentBomRef, string childBomRef)
        {
            int count = CountEdges(xml, parentBomRef, childBomRef);
            if (count != 1)
            {
                throw new XunitException($"Expected exactly one edge '{parentBomRef}' -> '{childBomRef}' but found {count}.");
            }
        }

        public static void DoesNotHaveDependency(string xml, string parentBomRef, string childBomRef)
        {
            int count = CountEdges(xml, parentBomRef, childBomRef);
            if (count != 0)
            {
                throw new XunitException($"Expected no edge '{parentBomRef}' -> '{childBomRef}' but found {count}.");
            }
        }

        public static void IsNotReferencedInDependencies(string xml, string bomRef)
        {
            var (document, ns) = Parse(xml);
            int count = document.Descendants(ns + "dependency").Count(e => (string)e.Attribute("ref") == bomRef);
            if (count != 0)
            {
                throw new XunitException($"Expected no <dependency ref=\"{bomRef}\"> elements but found {count}.");
            }
        }

        private static int CountComponents(string xml, string bomRef)
        {
            var (document, ns) = Parse(xml);
            return document.Descendants(ns + "component").Count(c => (string)c.Attribute("bom-ref") == bomRef);
        }

        private static int CountEdges(string xml, string parentBomRef, string childBomRef)
        {
            var (document, ns) = Parse(xml);
            var entries = document.Root.Element(ns + "dependencies")?.Elements(ns + "dependency")
                ?? Enumerable.Empty<XElement>();
            return entries
                .Where(e => (string)e.Attribute("ref") == parentBomRef)
                .Elements(ns + "dependency")
                .Count(e => (string)e.Attribute("ref") == childBomRef);
        }

        private static (XDocument Document, XNamespace Ns) Parse(string xml)
        {
            var document = XDocument.Parse(xml);
            if (document.Root == null)
            {
                throw new XunitException("XML document has no root element.");
            }

            return (document, document.Root.Name.Namespace);
        }
```

- [ ] **Step 3: Write the failing exclude tests**

Create `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineExcludeTests.cs`:

```csharp
using System.Xml.Linq;
using Cake.Core.Diagnostics;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxRefine;
using Cake.Testing;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxRefine;

public class CdxRefineExcludeTests
{
    private static readonly string Meta = TestSbom.Metadata("App", "1.0", "app");

    [Fact]
    public void ShouldExcludeComponentByName()
    {
        var xml = TestSbom.Create(Meta, TestSbom.Component("a", "Alpha") + TestSbom.Component("x", "xunit.core"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^xunit"));

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "x");
        AssertXml.HasComponent(refined, "a");
    }

    [Fact]
    public void ShouldExcludeComponentByPurl()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a", "Alpha", purl: "pkg:nuget/Alpha@1.0.0")
            + TestSbom.Component("x", "Analyzers", purl: "pkg:nuget/Microsoft.CodeAnalysis.Analyzers@3.0.0"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByPurl(@"^pkg:nuget/Microsoft\.CodeAnalysis\."));

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "x");
        AssertXml.HasComponent(refined, "a");
    }

    [Fact]
    public void ShouldExcludeComponentByBomRef()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("pkg:nuget/Alpha@1.0.0", "Alpha")
            + TestSbom.Component("pkg:nuget/Cake.Testing@5.0.0", "Cake.Testing"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByBomRef("^pkg:nuget/Cake\\.Testing@"));

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "pkg:nuget/Cake.Testing@5.0.0");
        AssertXml.HasComponent(refined, "pkg:nuget/Alpha@1.0.0");
    }

    [Fact]
    public void ShouldLeaveSbomUnchangedWhenNothingMatches()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a", "Alpha"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^Nothing$"));

        Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(refined)));
    }

    [Fact]
    public void ShouldRemoveNestedComponentsWithExcludedParent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("p", "Parent", nested: TestSbom.Component("n", "Nested")),
            TestSbom.Dependency("app", "p") + TestSbom.Dependency("p", "n") + TestSbom.Dependency("n"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^Parent$"));

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "p");
        AssertXml.DoesNotHaveComponent(refined, "n");
        AssertXml.IsNotReferencedInDependencies(refined, "p");
        AssertXml.IsNotReferencedInDependencies(refined, "n");
    }

    [Fact]
    public void ShouldExcludeNestedComponentOnItsOwn()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("p", "Parent", nested: TestSbom.Component("n", "Nested")),
            TestSbom.Dependency("app", "p") + TestSbom.Dependency("p", "n") + TestSbom.Dependency("n"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^Nested$"));

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "p");
        AssertXml.DoesNotHaveComponent(refined, "n");
        AssertXml.IsNotReferencedInDependencies(refined, "n");
    }

    [Fact]
    public void ShouldExcludeParentAndMatchingNestedChildOnce()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("p", "Parent", nested: TestSbom.Component("n", "Nested")),
            TestSbom.Dependency("app", "p") + TestSbom.Dependency("p", "n") + TestSbom.Dependency("n"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^(Parent|Nested)$"), log);

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "p");
        AssertXml.DoesNotHaveComponent(refined, "n");
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Information && e.Message == "Excluded 1 components.");
    }

    [Fact]
    public void ShouldRemoveDependencyEntryAndEdgesOfExcludedComponent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a", "Alpha") + TestSbom.Component("x", "Excluded") + TestSbom.Component("b", "Beta"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a", "x") + TestSbom.Dependency("x", "b") + TestSbom.Dependency("b"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^Excluded$"));

        AssertXml.IsValidSbom(refined);
        AssertXml.IsNotReferencedInDependencies(refined, "x");
        AssertXml.HasDependency(refined, "app", "a");
        AssertXml.HasComponent(refined, "b");
    }

    [Fact]
    public void ShouldKeepMetadataComponentAndWarn()
    {
        var xml = TestSbom.Create(
            TestSbom.Metadata("xunit.runner", "1.0", "app"),
            TestSbom.Component("x", "xunit.core"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithExcludeByName("^xunit"), log);

        AssertXml.IsValidSbom(refined);
        Assert.Equal("app", TestSbom.MetadataBomRef(refined));
        AssertXml.DoesNotHaveComponent(refined, "x");
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Warning
            && e.Message == "Metadata component 'xunit.runner' matches an exclusion rule but cannot be excluded.");
    }

    [Fact]
    public void ShouldApplyGroupAndTypeRulesOnlyToRemainingComponents()
    {
        var xml = TestSbom.Create(Meta, TestSbom.Component("a", "Alpha") + TestSbom.Component("x", "Excluded"));

        var settings = new CdxRefineSettings()
            .WithExcludeByName("^Excluded$")
            .WithGroupByName("Everything", ".*")
            .WithTypeByName("framework", ".*");
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "x");
        AssertXml.HaveComponentWithAttribute(refined, "a", "type", "framework");
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefineExcludeTests"`
Expected: build FAILS with `'CdxRefineSettings' does not contain a definition for 'WithExcludeByName'` (and the same for `WithExcludeByPurl` and `WithExcludeByBomRef`).

- [ ] **Step 5: Add the `Exclusions` setting and extension methods**

In `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettings.cs`, add as the first property of the class:

```csharp
    public List<ICdxComponentCriteria> Exclusions { get; set; } = new();
```

In `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs`, add at the top of the class body (before `WithGroupByName`):

```csharp
    public static CdxRefineSettings WithExcludeByName(this CdxRefineSettings settings, string namePattern)
    {
        settings.Exclusions.Add(new NameCriteria(namePattern));
        return settings;
    }

    public static CdxRefineSettings WithExcludeByPurl(this CdxRefineSettings settings, string purlPattern)
    {
        settings.Exclusions.Add(new PurlCriteria(purlPattern));
        return settings;
    }

    public static CdxRefineSettings WithExcludeByBomRef(this CdxRefineSettings settings, string bomRefPattern)
    {
        settings.Exclusions.Add(new BomRefCriteria(bomRefPattern));
        return settings;
    }
```

- [ ] **Step 6: Create the dependency graph**

Create `src/Cake.CycloneDX/Tools/CdxRefine/CdxDependencyGraph.cs`:

```csharp
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
```

Note: when a top-level entry is removed, its children are detached with it. A child that is also
in `matches` still has its (detached) parent, so calling `Remove()` on it is safe.

- [ ] **Step 7: Create the exclude step**

Create `src/Cake.CycloneDX/Tools/CdxRefine/ComponentExcluder.cs`:

```csharp
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
```

- [ ] **Step 8: Run exclusion from `CdxRefine`**

In `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAliases.cs`, in `CdxRefine(this ICakeContext context, XDocument document, ...)`, replace:

```csharp
        settings ??= new CdxRefineSettings();

        if (settings.GroupSettings.Any())
```

with:

```csharp
        settings ??= new CdxRefineSettings();

        var graph = new CdxDependencyGraph(document, ns);

        if (settings.Exclusions.Count > 0)
        {
            ComponentExcluder.Exclude(context, graph, settings.Exclusions);
        }

        if (settings.GroupSettings.Any())
```

- [ ] **Step 9: Run the tests to verify they pass**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefine"`
Expected: PASS for all `CdxRefineExcludeTests` and the existing `CdxRefineAliasTests`.

- [ ] **Step 10: Run the full build**

Run: `.\build.ps1 --target test`
Expected: build and all tests succeed on every target framework, with no StyleCop errors. Fix any analyzer error reported (typically missing braces or member ordering) and re-run.

- [ ] **Step 11: Commit**

```bash
git add src/Cake.CycloneDX/Tools/CdxRefine src/Cake.CycloneDX.Tests/Assertions/AssertXml.cs src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine
git commit -m ":sparkles: feat: Add component exclusion to CdxRefine" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Remove orphaned components

**Files:**
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxDependencyGraph.cs`
- Create: `src/Cake.CycloneDX/Tools/CdxRefine/OrphanedComponentRemover.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettings.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAliases.cs`
- Test: `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineRemoveOrphansTests.cs`

**Interfaces:**
- Consumes: from Task 1, `CdxDependencyGraph` (`Metadata`, `TopLevelComponents`, `GetBomRef`, `GetName`, `GetBomRefsIncludingNested`, `RemoveReferences`), `ComponentExcluder`, `WithExcludeByName`, and the `TestSbom` and `AssertXml` helpers.
- Produces:
  - `CdxRefineSettings.RemoveOrphanedComponents : bool`
  - `WithRemoveOrphanedComponents()` returning `CdxRefineSettings`
  - `CdxDependencyGraph.IsFlat : bool`, `MetadataBomRef : string?`, `IsAnchored : bool`, `ISet<string> GetReachable(string rootBomRef)`, `string GetDisplayName(XElement)`, and the private helpers `GetEntries(string)` and `GetChildren(string)`
  - `internal static class OrphanedComponentRemover` with `Remove(ICakeContext, CdxDependencyGraph)`

- [ ] **Step 1: Write the failing tests**

Create `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineRemoveOrphansTests.cs`:

```csharp
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxRefine;
using Cake.Testing;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxRefine;

public class CdxRefineRemoveOrphansTests
{
    private static readonly string Meta = TestSbom.Metadata("App", "1.0", "app");

    [Fact]
    public void ShouldRemoveOrphanChainLeftByExclusion()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("x") + TestSbom.Component("y"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a", "x") + TestSbom.Dependency("x", "y") + TestSbom.Dependency("y"));

        var settings = new CdxRefineSettings().WithExcludeByBomRef("^x$").WithRemoveOrphanedComponents();
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "a");
        AssertXml.DoesNotHaveComponent(refined, "y");
        AssertXml.IsNotReferencedInDependencies(refined, "y");
    }

    [Fact]
    public void ShouldRemovePreExistingUnreferencedComponent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("z"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a") + TestSbom.Dependency("z"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents());

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "a");
        AssertXml.DoesNotHaveComponent(refined, "z");
        AssertXml.IsNotReferencedInDependencies(refined, "z");
    }

    [Fact]
    public void ShouldRemoveDetachedCycle()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("x") + TestSbom.Component("y"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a") + TestSbom.Dependency("x", "y") + TestSbom.Dependency("y", "x"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents());

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "a");
        AssertXml.DoesNotHaveComponent(refined, "x");
        AssertXml.DoesNotHaveComponent(refined, "y");
    }

    [Fact]
    public void ShouldKeepComponentSharedWithKeptParent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("x") + TestSbom.Component("s"),
            TestSbom.Dependency("app", "a", "x") + TestSbom.Dependency("a", "s") + TestSbom.Dependency("x", "s") + TestSbom.Dependency("s"));

        var settings = new CdxRefineSettings().WithExcludeByBomRef("^x$").WithRemoveOrphanedComponents();
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "s");
        AssertXml.HasDependency(refined, "a", "s");
    }

    [Fact]
    public void ShouldRemoveEverythingBelowMetaWhenAllChildrenExcluded()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("b"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a", "b") + TestSbom.Dependency("b"));

        var settings = new CdxRefineSettings().WithExcludeByBomRef("^a$").WithRemoveOrphanedComponents();
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.DoesNotHaveComponent(refined, "a");
        AssertXml.DoesNotHaveComponent(refined, "b");
        AssertXml.IsNotReferencedInDependencies(refined, "b");
    }

    [Fact]
    public void ShouldRemoveTopLevelComponentWithoutBomRef()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component(null, "NoRef"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents());

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "a");
        Assert.DoesNotContain("<name>NoRef</name>", refined);
    }

    [Fact]
    public void ShouldKeepNestedComponentsOfReachableParent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("p", "Parent", nested: TestSbom.Component("n", "Nested")),
            TestSbom.Dependency("app", "p") + TestSbom.Dependency("p", "n") + TestSbom.Dependency("n"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents());

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "p");
        AssertXml.HasComponent(refined, "n");
    }

    [Fact]
    public void ShouldFollowEdgesAcrossDuplicateDependencyEntries()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("b"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a") + TestSbom.Dependency("a", "b") + TestSbom.Dependency("b"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents());

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "b");
    }

    [Fact]
    public void ShouldSkipOrphanRemovalForFlatSbom()
    {
        var xml = TestSbom.Create(Meta, TestSbom.Component("a") + TestSbom.Component("b"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents(), log);

        Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(refined)));
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Information
            && e.Message == "SBOM has no dependency graph; skipping orphan removal.");
    }

    [Fact]
    public void ShouldThrowWhenMetadataIsNotRootOfGraph()
    {
        var xml = TestSbom.Create(
            TestSbom.Metadata("Merged", "1.0"),
            TestSbom.Component("p1") + TestSbom.Component("p2") + TestSbom.Component("a") + TestSbom.Component("b"),
            TestSbom.Dependency("p1", "a") + TestSbom.Dependency("p2", "b") + TestSbom.Dependency("a") + TestSbom.Dependency("b"));

        var exception = Record.Exception(() => TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents()));

        Assert.IsType<CakeException>(exception);
        Assert.Equal(
            "Cannot remove orphaned components: the metadata component is not the root of the dependency graph. Use WithAdoptOrphanedComponents() or WithAdoptionBy*() to anchor the tree first.",
            exception.Message);
    }

    [Fact]
    public void ShouldThrowWhenSbomHasNoMetadataButHasGraph()
    {
        var xml = TestSbom.Create(
            null,
            TestSbom.Component("a") + TestSbom.Component("b"),
            TestSbom.Dependency("a", "b") + TestSbom.Dependency("b"));

        var exception = Record.Exception(() => TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents()));

        Assert.IsType<CakeException>(exception);
    }

    [Fact]
    public void ShouldLogRemovedOrphansLikeCycloneDxDotNet()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("z", "Zed", version: "2.0"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a") + TestSbom.Dependency("z"));
        var log = new FakeLog();

        TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents(), log);

        var warnings = log.Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message).ToList();
        Assert.Equal(new[] { "The following orphaned components have been removed:", "  - Zed@2.0" }, warnings);
    }

    [Fact]
    public void ShouldLogWhenNoOrphansWereFound()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));
        var log = new FakeLog();

        TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents(), log);

        Assert.Contains(log.Entries, e => e.Level == LogLevel.Verbose && e.Message == "No orphaned components were found.");
    }
}
```

Note on `ShouldFollowEdgesAcrossDuplicateDependencyEntries`: it contains two top-level
`<dependency ref="a">` entries. If `AssertXml.IsValidSbom` rejects that (a schema uniqueness
constraint on `ref`), keep the input as it is, remove only the `IsValidSbom` line from that test
and add a comment explaining why. The behavior under test is reachability, not schema validity.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefineRemoveOrphansTests"`
Expected: build FAILS with `'CdxRefineSettings' does not contain a definition for 'WithRemoveOrphanedComponents'`.

- [ ] **Step 3: Add the setting and extension method**

In `CdxRefineSettings.cs`, add after `Exclusions`:

```csharp
    public bool RemoveOrphanedComponents { get; set; }
```

In `CdxRefineSettingsExtensions.cs`, add after `WithExcludeByBomRef`:

```csharp
    public static CdxRefineSettings WithRemoveOrphanedComponents(this CdxRefineSettings settings)
    {
        settings.RemoveOrphanedComponents = true;
        return settings;
    }
```

- [ ] **Step 4: Extend the dependency graph**

In `CdxDependencyGraph.cs`:

Add these public properties after `AllComponents` (before the private `Dependencies` property):

```csharp
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
```

Add these public methods after `GetName`:

```csharp
    public string GetDisplayName(XElement component)
    {
        var version = component.Element(_ns + "version")?.Value;
        return string.IsNullOrWhiteSpace(version) ? GetName(component) : $"{GetName(component)}@{version}";
    }

    public ISet<string> GetReachable(string rootBomRef)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal) { rootBomRef };
        var queue = new Queue<string>();
        queue.Enqueue(rootBomRef);

        while (queue.Count > 0)
        {
            foreach (var child in GetChildren(queue.Dequeue()))
            {
                if (reachable.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return reachable;
    }
```

Add these private methods at the end of the class:

```csharp
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
```

`GetEntries` returns *all* top-level entries for a ref, which makes duplicate entries work (Review Focus 2).

- [ ] **Step 5: Create the remove-orphans step**

Create `src/Cake.CycloneDX/Tools/CdxRefine/OrphanedComponentRemover.cs`:

```csharp
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
```

- [ ] **Step 6: Run the step from `CdxRefine`**

In `CdxRefineAliases.cs`, directly after the `if (settings.Exclusions.Count > 0) { ... }` block, add:

```csharp
        if (settings.RemoveOrphanedComponents)
        {
            OrphanedComponentRemover.Remove(context, graph);
        }
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefine"`
Expected: PASS for all `CdxRefine*` tests.

- [ ] **Step 8: Run the full build**

Run: `.\build.ps1 --target test`
Expected: succeeds on every target framework with no StyleCop errors.

- [ ] **Step 9: Commit**

```bash
git add src/Cake.CycloneDX/Tools/CdxRefine src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine
git commit -m ":sparkles: feat: Add orphaned component removal to CdxRefine" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Adopt orphaned components

**Files:**
- Create: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAdoptionSettings.cs`
- Create: `src/Cake.CycloneDX/Tools/CdxRefine/ComponentAdopter.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxDependencyGraph.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettings.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAliases.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/NameCriteria.cs`, `PurlCriteria.cs`, `BomRefCriteria.cs`
- Test: `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineAdoptTests.cs`

**Interfaces:**
- Consumes: from Tasks 1–2, `CdxDependencyGraph` (`Namespace`, `Metadata`, `TopLevelComponents`, `AllComponents`, `IsFlat`, `GetBomRef`, `GetName`, `GetReachable`, private `GetEntries`/`GetChildren`), `WithRemoveOrphanedComponents`, and the `TestSbom` and `AssertXml` helpers.
- Produces:
  - `public record CdxRefineAdoptionSettings(ICdxComponentCriteria Criteria, ICdxComponentCriteria? Parent = null)`
  - `CdxRefineSettings.Adoptions : List<CdxRefineAdoptionSettings>`, `CdxRefineSettings.AdoptOrphanedComponents : bool`
  - `WithAdoptionByName(string namePattern, ICdxComponentCriteria? parent = null)`, `WithAdoptionByPurl(string purlPattern, ICdxComponentCriteria? parent = null)`, `WithAdoptionByBomRef(string bomRefPattern, ICdxComponentCriteria? parent = null)`, `WithAdoptOrphanedComponents()`
  - `CdxDependencyGraph.GetOrphans() : List<XElement>`, `IsReachable(string from, string to) : bool`, `AddEdge(string parent, string child) : bool`, `ContainsBomRef(string) : bool`
  - `ToString()` on `NameCriteria` (`name matches '{pattern}'`), `PurlCriteria` (`purl matches '{pattern}'`) and `BomRefCriteria` (`bom-ref matches '{pattern}'`)
  - `internal static class ComponentAdopter` with `Adopt(ICakeContext, CdxDependencyGraph, IReadOnlyList<CdxRefineAdoptionSettings>, bool adoptOrphanedComponents)`

- [ ] **Step 1: Write the failing tests**

Create `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefineAdoptTests.cs`:

```csharp
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxRefine;
using Cake.Testing;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxRefine;

public class CdxRefineAdoptTests
{
    private static readonly string Meta = TestSbom.Metadata("App", "1.0", "app");

    // Shape produced by a flat cyclonedx-cli merge: meta has no bom-ref and no edges.
    private static readonly string FlatMerge = TestSbom.Create(
        TestSbom.Metadata("Merged", "1.0"),
        TestSbom.Component("p1", "Project1") + TestSbom.Component("p2", "Project2") + TestSbom.Component("a") + TestSbom.Component("b"),
        TestSbom.Dependency("p1", "a") + TestSbom.Dependency("p2", "b") + TestSbom.Dependency("a") + TestSbom.Dependency("b"));

    [Fact]
    public void ShouldAdoptOrphansIntoMetadataOnFlatMergeShape()
    {
        var refined = TestSbom.Refine(FlatMerge, new CdxRefineSettings().WithAdoptOrphanedComponents());

        AssertXml.IsValidSbom(refined);
        Assert.Equal("Merged@1.0", TestSbom.MetadataBomRef(refined));
        AssertXml.HasDependency(refined, "Merged@1.0", "p1");
        AssertXml.HasDependency(refined, "Merged@1.0", "p2");
        AssertXml.DoesNotHaveDependency(refined, "Merged@1.0", "a");
        AssertXml.DoesNotHaveDependency(refined, "Merged@1.0", "b");
    }

    [Fact]
    public void ShouldMakeOrphanRemovalSucceedAfterAdoption()
    {
        var settings = new CdxRefineSettings().WithAdoptOrphanedComponents().WithRemoveOrphanedComponents();

        var refined = TestSbom.Refine(FlatMerge, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasComponent(refined, "p1");
        AssertXml.HasComponent(refined, "p2");
        AssertXml.HasComponent(refined, "a");
        AssertXml.HasComponent(refined, "b");
    }

    [Fact]
    public void ShouldUseNameWithoutVersionForMetadataBomRef()
    {
        var xml = TestSbom.Create(
            TestSbom.Metadata("Merged"),
            TestSbom.Component("p1") + TestSbom.Component("a"),
            TestSbom.Dependency("p1", "a") + TestSbom.Dependency("a"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptOrphanedComponents());

        Assert.Equal("Merged", TestSbom.MetadataBomRef(refined));
        AssertXml.HasDependency(refined, "Merged", "p1");
    }

    [Fact]
    public void ShouldAdoptByRuleIntoMetadataByDefault()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("p1", "Project1") + TestSbom.Component("p2", "Project2"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptionByName("^Project1$"));

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "app", "p1");
        AssertXml.DoesNotHaveDependency(refined, "app", "p2");
    }

    [Fact]
    public void ShouldAdoptByRuleIntoExplicitParent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("foo", "Foo") + TestSbom.Component("plugin", "Foo.Plugin", purl: "pkg:nuget/Foo.Plugin@1.0.0"),
            TestSbom.Dependency("app", "foo") + TestSbom.Dependency("foo"));

        var settings = new CdxRefineSettings().WithAdoptionByPurl(@"^pkg:nuget/Foo\.Plugin", parent: new NameCriteria("^Foo$"));
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "foo", "plugin");
        AssertXml.DoesNotHaveDependency(refined, "app", "plugin");
    }

    [Fact]
    public void ShouldAdoptByBomRef()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("pkg:nuget/Orphan@1.0.0", "Orphan"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptionByBomRef("^pkg:nuget/Orphan@"));

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "app", "pkg:nuget/Orphan@1.0.0");
    }

    [Fact]
    public void ShouldLetFirstMatchingRuleWin()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("foo", "Foo") + TestSbom.Component("plugin", "Foo.Plugin") + TestSbom.Component("z", "Zed"),
            TestSbom.Dependency("app", "foo") + TestSbom.Dependency("foo"));

        var settings = new CdxRefineSettings()
            .WithAdoptionByName(@"^Foo\.Plugin$", parent: new NameCriteria("^Foo$"))
            .WithAdoptionByName(@"^Foo\.Plugin$")
            .WithAdoptOrphanedComponents();
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "foo", "plugin");
        AssertXml.DoesNotHaveDependency(refined, "app", "plugin");
        AssertXml.HasDependency(refined, "app", "z");
    }

    [Fact]
    public void ShouldSkipComponentsThatAreNotOrphans()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a", "Alpha") + TestSbom.Component("b", "Beta"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a", "b") + TestSbom.Dependency("b"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptionByName(".*"), log);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "app", "a");
        AssertXml.DoesNotHaveDependency(refined, "app", "b");
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Verbose
            && e.Message == "Skipping adoption of component 'Beta': it is not an orphan.");
    }

    [Fact]
    public void ShouldSkipParentWhenRuleMatchesIt()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("foo", "Foo") + TestSbom.Component("plugin", "Foo.Plugin"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var settings = new CdxRefineSettings().WithAdoptionByName("^Foo", parent: new NameCriteria("^Foo$"));
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "foo", "plugin");
        AssertXml.DoesNotHaveDependency(refined, "foo", "foo");
    }

    [Fact]
    public void ShouldThrowWhenParentCriteriaMatchesNoComponent()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("o", "Orphan"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var settings = new CdxRefineSettings().WithAdoptionByName("^Orphan$", parent: new NameCriteria("^Nothing$"));
        var exception = Record.Exception(() => TestSbom.Refine(xml, settings));

        Assert.IsType<CakeException>(exception);
        Assert.Equal("Adoption parent criteria (name matches '^Nothing$') must match exactly one component, but matched 0.", exception.Message);
    }

    [Fact]
    public void ShouldThrowWhenParentCriteriaMatchesSeveralComponents()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a", "Lib.A") + TestSbom.Component("b", "Lib.B") + TestSbom.Component("o", "Orphan"),
            TestSbom.Dependency("app", "a", "b") + TestSbom.Dependency("a") + TestSbom.Dependency("b"));

        var settings = new CdxRefineSettings().WithAdoptionByName("^Orphan$", parent: new NameCriteria("^Lib"));
        var exception = Record.Exception(() => TestSbom.Refine(xml, settings));

        Assert.IsType<CakeException>(exception);
        Assert.Equal("Adoption parent criteria (name matches '^Lib') must match exactly one component, but matched 2.", exception.Message);
    }

    [Fact]
    public void ShouldThrowWhenParentHasNoBomRef()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component(null, "NoRef") + TestSbom.Component("o", "Orphan"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var settings = new CdxRefineSettings().WithAdoptionByName("^Orphan$", parent: new NameCriteria("^NoRef$"));
        var exception = Record.Exception(() => TestSbom.Refine(xml, settings));

        Assert.IsType<CakeException>(exception);
        Assert.Equal("Adoption parent 'NoRef' has no bom-ref.", exception.Message);
    }

    [Fact]
    public void ShouldThrowWhenAdoptionWouldCreateCycle()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("o", "Orphan") + TestSbom.Component("p", "Child"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a") + TestSbom.Dependency("o", "p") + TestSbom.Dependency("p"));

        var settings = new CdxRefineSettings().WithAdoptionByName("^Orphan$", parent: new NameCriteria("^Child$"));
        var exception = Record.Exception(() => TestSbom.Refine(xml, settings));

        Assert.IsType<CakeException>(exception);
        Assert.Equal("Cannot adopt component 'o' into 'p': 'p' is a dependency of 'o', so the adoption would create a cycle.", exception.Message);
    }

    [Fact]
    public void ShouldThrowWhenGeneratedMetadataBomRefCollides()
    {
        var xml = TestSbom.Create(
            TestSbom.Metadata("Merged", "1.0"),
            TestSbom.Component("Merged@1.0", "Clash") + TestSbom.Component("a"),
            TestSbom.Dependency("Merged@1.0", "a") + TestSbom.Dependency("a"));

        var exception = Record.Exception(() => TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptOrphanedComponents()));

        Assert.IsType<CakeException>(exception);
        Assert.Equal("Cannot assign bom-ref 'Merged@1.0' to the metadata component: another component already uses it.", exception.Message);
    }

    [Fact]
    public void ShouldThrowWhenAdoptingIntoMissingMetadata()
    {
        var xml = TestSbom.Create(
            null,
            TestSbom.Component("a") + TestSbom.Component("b"),
            TestSbom.Dependency("a", "b") + TestSbom.Dependency("b"));

        var exception = Record.Exception(() => TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptOrphanedComponents()));

        Assert.IsType<CakeException>(exception);
        Assert.Equal("Cannot adopt into the metadata component: the SBOM has no metadata component.", exception.Message);
    }

    [Fact]
    public void ShouldNotAssignMetadataBomRefWhenNothingIsAdopted()
    {
        var refined = TestSbom.Refine(FlatMerge, new CdxRefineSettings().WithAdoptionByName("^Nothing$"));

        Assert.Null(TestSbom.MetadataBomRef(refined));
    }

    [Fact]
    public void ShouldSkipAdoptionForFlatSbom()
    {
        var xml = TestSbom.Create(TestSbom.Metadata("Merged", "1.0"), TestSbom.Component("a") + TestSbom.Component("b"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptOrphanedComponents(), log);

        Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(refined)));
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Information
            && e.Message == "SBOM has no dependency graph; skipping adoption.");
    }

    [Fact]
    public void ShouldLogAdoptions()
    {
        var log = new FakeLog();

        TestSbom.Refine(FlatMerge, new CdxRefineSettings().WithAdoptOrphanedComponents(), log);

        Assert.Contains(log.Entries, e => e.Level == LogLevel.Verbose
            && e.Message == "Adopting component 'Project1' (p1) into 'Merged' (Merged@1.0)");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefineAdoptTests"`
Expected: build FAILS with `'CdxRefineSettings' does not contain a definition for 'WithAdoptOrphanedComponents'` (and the same for the `WithAdoptionBy*` methods).

- [ ] **Step 3: Add the settings record, properties and extension methods**

Create `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAdoptionSettings.cs`:

```csharp
namespace Cake.CycloneDX.Tools.CdxRefine;

public record CdxRefineAdoptionSettings(ICdxComponentCriteria Criteria, ICdxComponentCriteria? Parent = null);
```

In `CdxRefineSettings.cs`, add as the first two properties (before `Exclusions`):

```csharp
    public List<CdxRefineAdoptionSettings> Adoptions { get; set; } = new();
    public bool AdoptOrphanedComponents { get; set; }
```

In `CdxRefineSettingsExtensions.cs`, add at the top of the class body (before `WithExcludeByName`):

```csharp
    public static CdxRefineSettings WithAdoptionByName(this CdxRefineSettings settings, string namePattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new NameCriteria(namePattern), parent));
        return settings;
    }

    public static CdxRefineSettings WithAdoptionByPurl(this CdxRefineSettings settings, string purlPattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new PurlCriteria(purlPattern), parent));
        return settings;
    }

    public static CdxRefineSettings WithAdoptionByBomRef(this CdxRefineSettings settings, string bomRefPattern, ICdxComponentCriteria? parent = null)
    {
        settings.Adoptions.Add(new CdxRefineAdoptionSettings(new BomRefCriteria(bomRefPattern), parent));
        return settings;
    }

    public static CdxRefineSettings WithAdoptOrphanedComponents(this CdxRefineSettings settings)
    {
        settings.AdoptOrphanedComponents = true;
        return settings;
    }
```

- [ ] **Step 4: Add `ToString()` to the criteria**

In `NameCriteria.cs`, add after `IsMatch`:

```csharp
    public override string ToString()
    {
        return $"name matches '{_pattern}'";
    }
```

In `PurlCriteria.cs`, add after `IsMatch`:

```csharp
    public override string ToString()
    {
        return $"purl matches '{_pattern}'";
    }
```

In `BomRefCriteria.cs`, add after `IsMatch`:

```csharp
    public override string ToString()
    {
        return $"bom-ref matches '{_pattern}'";
    }
```

(`Regex.ToString()` returns the pattern.)

- [ ] **Step 5: Extend the dependency graph**

In `CdxDependencyGraph.cs`, add these public methods after `GetReachable`:

```csharp
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
```

- [ ] **Step 6: Create the adopt step**

Create `src/Cake.CycloneDX/Tools/CdxRefine/ComponentAdopter.cs`:

```csharp
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
```

Notes for the implementer:
- The parent is resolved only when a rule has something to adopt. That is why a rule matching nothing leaves meta without a bom-ref (Review Focus 5).
- Orphans are collected once, before any edge is added. An orphan that serves as an explicit parent can still be adopted into meta afterwards by `AdoptOrphanedComponents`.

- [ ] **Step 7: Run adoption first in `CdxRefine`**

In `CdxRefineAliases.cs`, directly after `var graph = new CdxDependencyGraph(document, ns);`, add:

```csharp
        if (settings.Adoptions.Count > 0 || settings.AdoptOrphanedComponents)
        {
            ComponentAdopter.Adopt(context, graph, settings.Adoptions, settings.AdoptOrphanedComponents);
        }
```

The method should now read, in order: adopt block, exclusions block, orphan-removal block, then the existing group and type blocks.

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefine"`
Expected: PASS for all `CdxRefine*` tests.

- [ ] **Step 9: Run the full build**

Run: `.\build.ps1 --target test`
Expected: succeeds on every target framework with no StyleCop errors.

- [ ] **Step 10: Commit**

```bash
git add src/Cake.CycloneDX/Tools/CdxRefine src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine
git commit -m ":sparkles: feat: Add orphan adoption to CdxRefine" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Pipeline test and parameter rename

**Files:**
- Test: `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefinePipelineTests.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs` (`WithTypeByPurl`)

**Interfaces:**
- Consumes: every `With…` method from Tasks 1–3, the existing `WithGroupByName`, and the `TestSbom` and `AssertXml` helpers.
- Produces: nothing new. `WithTypeByPurl(string typeName, string purlPattern)` gets a corrected parameter name.

- [ ] **Step 1: Write the pipeline test**

Create `src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefinePipelineTests.cs`:

```csharp
using System.Xml.Linq;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxRefine;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxRefine;

public class CdxRefinePipelineTests
{
    [Fact]
    public void ShouldAnchorExcludeAndPruneFlatMergedSbom()
    {
        var xml = TestSbom.Create(
            TestSbom.Metadata("Merged", "1.0"),
            TestSbom.Component("p1", "Project1")
            + TestSbom.Component("p2", "Project2")
            + TestSbom.Component("core", "Core")
            + TestSbom.Component("xunit", "xunit")
            + TestSbom.Component("xabs", "xunit.abstractions"),
            TestSbom.Dependency("p1", "core", "xunit")
            + TestSbom.Dependency("p2", "core")
            + TestSbom.Dependency("core")
            + TestSbom.Dependency("xunit", "xabs")
            + TestSbom.Dependency("xabs"));

        var settings = new CdxRefineSettings()
            .WithAdoptOrphanedComponents()
            .WithExcludeByName("^xunit$")
            .WithRemoveOrphanedComponents()
            .WithGroupByName("Libraries", "^Core$");
        var refined = TestSbom.Refine(xml, settings);

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "Merged@1.0", "p1");
        AssertXml.HasDependency(refined, "Merged@1.0", "p2");
        AssertXml.HasComponent(refined, "core");
        AssertXml.DoesNotHaveComponent(refined, "xunit");
        AssertXml.DoesNotHaveComponent(refined, "xabs");
        AssertXml.IsNotReferencedInDependencies(refined, "xunit");
        AssertXml.IsNotReferencedInDependencies(refined, "xabs");

        var document = XDocument.Parse(refined);
        XNamespace ns = document.Root.Name.Namespace;
        var core = document.Descendants(ns + "component").Single(c => (string)c.Attribute("bom-ref") == "core");
        Assert.Equal("Libraries", core.Element(ns + "group")?.Value);
    }
}
```

- [ ] **Step 2: Run the test**

Run: `dotnet test src/Cake.CycloneDX.Tests -f net9.0 --filter "FullyQualifiedName~CdxRefinePipelineTests"`
Expected: PASS. This test checks that Tasks 1–3 work together, so it passes without new production code. If it fails, the step order in `CdxRefineAliases` is wrong: it must be adopt, then exclude, then remove orphans, then group and type.

- [ ] **Step 3: Rename the misnamed parameter**

In `CdxRefineSettingsExtensions.cs`, replace:

```csharp
    public static CdxRefineSettings WithTypeByPurl(this CdxRefineSettings settings, string typeName, string bomRefPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new PurlCriteria(bomRefPattern)));
        return settings;
    }
```

with:

```csharp
    public static CdxRefineSettings WithTypeByPurl(this CdxRefineSettings settings, string typeName, string purlPattern)
    {
        settings.TypeSettings.Add(new CdxRefineTypeSettings(typeName, new PurlCriteria(purlPattern)));
        return settings;
    }
```

- [ ] **Step 4: Run the full build**

Run: `.\build.ps1 --target test`
Expected: build and all tests succeed on every target framework with no StyleCop errors.

- [ ] **Step 5: Commit**

```bash
git add src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineSettingsExtensions.cs src/Cake.CycloneDX.Tests/Unit/Tools/CdxRefine/CdxRefinePipelineTests.cs
git commit -m ":white_check_mark: test: Add CdxRefine pipeline test and fix WithTypeByPurl parameter name" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
