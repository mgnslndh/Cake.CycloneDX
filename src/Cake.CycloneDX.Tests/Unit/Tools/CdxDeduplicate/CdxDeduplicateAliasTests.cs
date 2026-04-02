using Cake.Core;
using Cake.Core.IO;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxDeduplicate;
using Cake.Testing;
using NSubstitute;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxDeduplicate;

public class CdxDeduplicateAliasTests
{
    [Fact]
    public void DuplicateComponentByPurlShouldBeInvalid()
    {
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components>
                           <component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           <component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           </components>
                           </bom>
                           """;

        Assert.Throws<Xunit.Sdk.XunitException>(() => AssertXml.IsValidSbom(xml));
    }

    [Fact]
    public void ShouldDeduplicateByBomRef()
    {
        const string xml = """
<bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
<components>
<component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
  <name>Cake.Core</name>
</component>
<component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
  <name>Cake.Core</name>
</component>
</components>
</bom>
""";
        var context = Substitute.For<ICakeContext>();
        var deduplicated = context.CdxDeduplicate(xml);
        AssertXml.IsValidSbom(deduplicated);
    }

    [Fact]
    public void ShouldDeduplicateByPurl()
    {
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components>
                           <component type="library">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           <component type="library">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           </components>
                           </bom>
                           """;
        var context = Substitute.For<ICakeContext>();
        var deduplicated = context.CdxDeduplicate(xml);
        AssertXml.IsValidSbom(deduplicated);
        AssertXml.HaveSingleComponentWithPurl(deduplicated, "pkg:nuget/Cake.Core@5.0.0");
    }

    [Fact]
    public void ShouldRewriteDependencyRefsWhenDeduplicatingByPurl()
    {
        // Two components share the same PURL but have different bom-ref values.
        // A dependency references the second component (bom-ref="comp-b"), which is the one
        // that gets discarded. After deduplication the dependency ref must point to the retained
        // component (bom-ref="comp-a").
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components>
                           <component type="library" bom-ref="comp-a">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           <component type="library" bom-ref="comp-b">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           </components>
                           <dependencies>
                             <dependency ref="comp-b" />
                           </dependencies>
                           </bom>
                           """;
        var context = Substitute.For<ICakeContext>();
        var result = context.CdxDeduplicate(xml, new CdxDeduplicateSettings { DeduplicateByPurl = true, DeduplicateByBomRef = false });

        var doc = System.Xml.Linq.XDocument.Parse(result);
        System.Xml.Linq.XNamespace ns = doc.Root!.Name.Namespace;
        var depRef = doc.Descendants(ns + "dependency").Single().Attribute("ref")?.Value;
        Assert.Equal("comp-a", depRef);
    }

    [Fact]
    public void ShouldPreserveDependencyRefsWhenDeduplicatingByBomRef()
    {
        // Two components share the same bom-ref. A dependency references that bom-ref.
        // After deduplication the dependency ref must be unchanged because the retained
        // component keeps the same bom-ref value.
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components>
                           <component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
                             <name>Cake.Core</name>
                           </component>
                           <component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
                             <name>Cake.Core</name>
                           </component>
                           </components>
                           <dependencies>
                             <dependency ref="pkg:nuget/Cake.Core@5.0.0" />
                           </dependencies>
                           </bom>
                           """;
        var context = Substitute.For<ICakeContext>();
        var result = context.CdxDeduplicate(xml, new CdxDeduplicateSettings { DeduplicateByBomRef = true, DeduplicateByPurl = false });

        var doc = System.Xml.Linq.XDocument.Parse(result);
        System.Xml.Linq.XNamespace ns = doc.Root!.Name.Namespace;
        var depRef = doc.Descendants(ns + "dependency").Single().Attribute("ref")?.Value;
        Assert.Equal("pkg:nuget/Cake.Core@5.0.0", depRef);
    }

    [Fact]
    public void ShouldThrowCakeExceptionIfInputFileDoesNotExist()
    {
        var environment = FakeEnvironment.CreateUnixEnvironment();
        var fileSystem = new FakeFileSystem(environment);
        var context = Substitute.For<ICakeContext>();
        context.FileSystem.Returns(fileSystem);

        var result = Record.Exception(() =>
            context.CdxDeduplicate(new FilePath("/input.xml"), new FilePath("/output.xml")));

        Assert.IsType<CakeException>(result);
        Assert.Contains("/input.xml", result.Message);
    }

    [Fact]
    public void ShouldCreateOutputDirectoryIfMissing()
    {
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components />
                           </bom>
                           """;
        var environment = FakeEnvironment.CreateUnixEnvironment();
        var fileSystem = new FakeFileSystem(environment);
        fileSystem.CreateFile("/input.xml").SetContent(xml);
        var context = Substitute.For<ICakeContext>();
        context.FileSystem.Returns(fileSystem);

        context.CdxDeduplicate(new FilePath("/input.xml"), new FilePath("/subdir/output.xml"));

        Assert.True(fileSystem.GetFile(new FilePath("/subdir/output.xml")).Exists);
    }

    [Fact]
    public void ShouldLoadAndSaveViaFileSystem()
    {
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components>
                           <component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           </components>
                           </bom>
                           """;
        var environment = FakeEnvironment.CreateUnixEnvironment();
        var fileSystem = new FakeFileSystem(environment);
        fileSystem.CreateFile("/input.xml").SetContent(xml);
        var context = Substitute.For<ICakeContext>();
        context.FileSystem.Returns(fileSystem);

        context.CdxDeduplicate(new FilePath("/input.xml"), new FilePath("/output.xml"));

        var outputFile = fileSystem.GetFile(new FilePath("/output.xml"));
        Assert.True(outputFile.Exists);
        string outputXml;
        using (var stream = outputFile.OpenRead())
        {
            using (var reader = new System.IO.StreamReader(stream))
            {
                outputXml = reader.ReadToEnd();
            }
        }
        AssertXml.IsValidSbom(outputXml);
    }
}