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
