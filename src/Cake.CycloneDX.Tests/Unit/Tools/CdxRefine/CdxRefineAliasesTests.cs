using Cake.Core;
using Cake.CycloneDX.Tests.Assertions;
using Cake.CycloneDX.Tools.CdxDeduplicate;
using Cake.CycloneDX.Tools.CdxRefine;
using NSubstitute;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit.Tools.CdxRefine;

public class CdxRefineAliasTests
{
    [Fact]
    public void ShouldChangeTypeFromLibraryToDevice()
    {
        const string xml = """
                           <bom xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" serialNumber="urn:uuid:dc1e8435-1749-4a34-b81a-17d3a56f9032" version="1" xmlns="http://cyclonedx.org/schema/bom/1.6">
                           <components>
                           <component type="library" bom-ref="pkg:nuget/Cake.Core@5.0.0">
                             <name>Cake.Core</name>
                             <purl>pkg:nuget/Cake.Core@5.0.0</purl>
                           </component>
                           <component type="application" bom-ref="pkg:nuget/Cake.Common@5.0.0">
                             <name>Cake.Common</name>
                             <purl>pkg:nuget/Cake.Common@5.0.0</purl>
                           </component>
                           </components>
                           </bom>
                           """;

        var context = Substitute.For<ICakeContext>();
        var settings = new CdxRefineSettings()
            .WithTypeByBomRef("device", "pkg:nuget/Cake.Core@5.0.0");
        var refined = context.CdxRefine(xml, settings);
        AssertXml.IsValidSbom(refined);
        AssertXml.HaveComponentWithAttribute(refined, "pkg:nuget/Cake.Core@5.0.0", "type", "device");
    }
}