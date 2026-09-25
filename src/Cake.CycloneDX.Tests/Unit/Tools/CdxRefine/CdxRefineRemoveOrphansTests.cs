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

        AssertXml.IsValidSbom(refined);
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

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents(), log);

        AssertXml.IsValidSbom(refined);
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

        var refined = TestSbom.Refine(xml, new CdxRefineSettings().WithRemoveOrphanedComponents(), log);

        AssertXml.IsValidSbom(refined);
        Assert.Contains(log.Entries, e => e.Level == LogLevel.Verbose && e.Message == "No orphaned components were found.");
    }
}
