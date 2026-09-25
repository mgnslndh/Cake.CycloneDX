using System.Diagnostics;
using System.Text;
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

    [Fact]
    public void ShouldPruneLargeFlatMergedSbomWithinPerformanceBudget()
    {
        const int ProjectCount = 20;
        const int ChainLength = 3;
        const int FanCountPerProject = 247; // ChainLength + FanCountPerProject == 250 components/project.

        var componentsXml = new StringBuilder();
        var dependenciesXml = new StringBuilder();

        for (var i = 0; i < ProjectCount; i++)
        {
            var projectRef = $"P{i}";
            componentsXml.Append(TestSbom.Component(projectRef, $"Project{i}"));

            var chainRefs = new string[ChainLength];
            for (var c = 0; c < ChainLength; c++)
            {
                chainRefs[c] = $"P{i}_c{c}";
                componentsXml.Append(TestSbom.Component(chainRefs[c]));
            }

            var fanRefs = new string[FanCountPerProject];
            for (var f = 0; f < FanCountPerProject; f++)
            {
                fanRefs[f] = $"P{i}_f{f}";
                componentsXml.Append(TestSbom.Component(fanRefs[f]));
            }

            dependenciesXml.Append(TestSbom.Dependency(projectRef, new[] { chainRefs[0] }.Concat(fanRefs).ToArray()));

            for (var c = 0; c < ChainLength - 1; c++)
            {
                dependenciesXml.Append(TestSbom.Dependency(chainRefs[c], chainRefs[c + 1]));
            }

            dependenciesXml.Append(TestSbom.Dependency(chainRefs[ChainLength - 1]));

            foreach (var fanRef in fanRefs)
            {
                dependenciesXml.Append(TestSbom.Dependency(fanRef));
            }
        }

        var xml = TestSbom.Create(TestSbom.Metadata("Merged", "1.0"), componentsXml.ToString(), dependenciesXml.ToString());

        var settings = new CdxRefineSettings()
            .WithAdoptOrphanedComponents()
            .WithExcludeByBomRef("^P0_c0$")
            .WithRemoveOrphanedComponents();

        var stopwatch = Stopwatch.StartNew();
        var refined = TestSbom.Refine(xml, settings);
        stopwatch.Stop();

        AssertXml.IsValidSbom(refined);
        AssertXml.HasDependency(refined, "Merged@1.0", "P0");
        AssertXml.HasDependency(refined, "Merged@1.0", "P19");
        AssertXml.DoesNotHaveComponent(refined, "P0_c0");
        AssertXml.DoesNotHaveComponent(refined, "P0_c1");
        AssertXml.DoesNotHaveComponent(refined, "P0_c2");
        AssertXml.HasComponent(refined, "P0_f0");
        AssertXml.HasComponent(refined, "P1_c0");

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Expected the refine pipeline to finish within 5 seconds, but took {stopwatch.Elapsed}.");
    }
}
