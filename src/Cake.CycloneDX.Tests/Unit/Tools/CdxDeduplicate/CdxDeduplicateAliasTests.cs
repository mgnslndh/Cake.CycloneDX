using Cake.Core;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxDeduplicate;
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
}