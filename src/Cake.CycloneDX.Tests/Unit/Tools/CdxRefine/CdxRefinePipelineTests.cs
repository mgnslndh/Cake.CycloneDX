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
