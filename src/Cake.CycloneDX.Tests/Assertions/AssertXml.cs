using System.Xml.Linq;
using CycloneDX;
using Xunit.Sdk;

namespace Cake.CycloneDX.Tests.Assertions
{
    internal class AssertXml
    {
        public static void IsValidSbom(string xml, global::CycloneDX.SpecificationVersion version = SpecificationVersion.v1_6)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new ArgumentException("XML cannot be null or empty.", nameof(xml));
            }

            var result = global::CycloneDX.Xml.Validator.Validate(xml, SpecificationVersion.v1_6);

            if (result.Valid == false)
            {
                var errors = string.Join(Environment.NewLine, result.Messages);
                throw new XunitException(errors);
            }
        }

        public static void HaveSingleComponentWithPurl(string xml, string expectedPurl)
        {
            var document = XDocument.Parse(xml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            
            if (document.Root == null)
            {
                throw new XunitException("XML document has no root element.");
            }
            
            XNamespace ns = document.Root.Name.Namespace;
            var componentsParent = document.Descendants(ns + "components").SingleOrDefault()
                ?? throw new XunitException("Expected exactly one 'components' element in the SBOM.");

            int count = componentsParent
                .Descendants(ns + "component")
                .Select(c => c.Element(ns + "purl")?.Value?.Trim())
                .Count(purl => purl == expectedPurl);

            if (count != 1)
            {
                throw new XunitException($"Expected exactly one component with PURL '{expectedPurl}', but found {count}.");
            }
        }
    }
}
