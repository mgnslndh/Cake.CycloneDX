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

        AssertXml.IsValidSbom(refined);
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
    public void ShouldLogNoBomRefWhenMatchedComponentHasNoBomRef()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component(null, "NoRef"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptionByName("^NoRef$"), log);

        AssertXml.IsValidSbom(refined);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Verbose
            && e.Message == "Skipping adoption of component 'NoRef': it has no bom-ref.");
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

        AssertXml.IsValidSbom(refined);
        Assert.Null(TestSbom.MetadataBomRef(refined));
    }

    [Fact]
    public void ShouldNotDuplicateEdgeWhenSameBomRefAppearsTwice()
    {
        var xml = TestSbom.Create(
            Meta,
            TestSbom.Component("a") + TestSbom.Component("dup", "Dup1") + TestSbom.Component("dup", "Dup2"),
            TestSbom.Dependency("app", "a") + TestSbom.Dependency("a"));

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptOrphanedComponents());

        // Duplicate bom-refs are not valid CycloneDX (bom-ref is an xs:ID and must be document-wide
        // unique), so AssertXml.IsValidSbom is intentionally not called here; this test only
        // exercises that adopting two orphans with the same bom-ref does not duplicate the edge.
        AssertXml.HasDependency(refined, "app", "dup");
    }

    [Fact]
    public void ShouldSkipAdoptionForFlatSbom()
    {
        var xml = TestSbom.Create(TestSbom.Metadata("Merged", "1.0"), TestSbom.Component("a") + TestSbom.Component("b"));
        var log = new FakeLog();

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithAdoptOrphanedComponents(), log);

        AssertXml.IsValidSbom(refined);
        Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(refined)));
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Information
            && e.Message == "SBOM has no dependency graph; skipping adoption.");
    }

    [Fact]
    public void ShouldLogAdoptions()
    {
        var log = new FakeLog();

        var refined = TestSbom.Refine(FlatMerge, new CdxRefineSettings().WithAdoptOrphanedComponents(), log);

        AssertXml.IsValidSbom(refined);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Verbose
            && e.Message == "Adopting component 'Project1' (p1) into 'Merged' (Merged@1.0)");
    }
}
