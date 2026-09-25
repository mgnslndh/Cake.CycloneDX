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
