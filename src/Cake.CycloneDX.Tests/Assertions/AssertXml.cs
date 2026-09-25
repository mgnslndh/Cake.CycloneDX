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

            var result = global::CycloneDX.Xml.Validator.Validate(xml, version);

            if (result.Valid == false)
            {
                var errors = string.Join(Environment.NewLine, result.Messages);
                throw new XunitException(errors);
            }
        }

        public static void HaveSingleComponentWithPurl(string xml, string expectedPurl)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new ArgumentException("XML cannot be null or empty.", nameof(xml));
            }

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

        public static void HaveComponentWithAttribute(string xml, string bomRef, string attributeName, string expectedValue)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new ArgumentException("XML cannot be null or empty.", nameof(xml));
            }

            var doc = XDocument.Parse(xml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);

            if (doc.Root == null)
            {
                throw new XunitException("XML document has no root element.");
            }

            XNamespace ns = doc.Root.Name.Namespace;

            var component = doc.Descendants(ns + "component")
                .FirstOrDefault(e => (string)e.Attribute("bom-ref") == bomRef);

            if (component == null)
            {
                throw new XunitException(
                    $"Expected component with bom-ref '{bomRef}' but it does not exist");
            }

            string actualValue = component.Attribute(attributeName)?.Value;

            if (expectedValue != actualValue)
            {
                throw new XunitException(
                    $"Expected component with bom-ref '{bomRef}' to have attribute {attributeName} with value '{expectedValue}' but it was '{actualValue ?? "<null>"}");
            }
        }

        public static void HasComponent(string xml, string bomRef)
        {
            if (CountComponents(xml, bomRef) == 0)
            {
                throw new XunitException($"Expected component with bom-ref '{bomRef}' but it does not exist.");
            }
        }

        public static void DoesNotHaveComponent(string xml, string bomRef)
        {
            int count = CountComponents(xml, bomRef);
            if (count != 0)
            {
                throw new XunitException($"Expected no component with bom-ref '{bomRef}' but found {count}.");
            }
        }

        public static void HasDependency(string xml, string parentBomRef, string childBomRef)
        {
            int count = CountEdges(xml, parentBomRef, childBomRef);
            if (count != 1)
            {
                throw new XunitException($"Expected exactly one edge '{parentBomRef}' -> '{childBomRef}' but found {count}.");
            }
        }

        public static void DoesNotHaveDependency(string xml, string parentBomRef, string childBomRef)
        {
            int count = CountEdges(xml, parentBomRef, childBomRef);
            if (count != 0)
            {
                throw new XunitException($"Expected no edge '{parentBomRef}' -> '{childBomRef}' but found {count}.");
            }
        }

        public static void IsNotReferencedInDependencies(string xml, string bomRef)
        {
            var (document, ns) = Parse(xml);
            int count = document.Descendants(ns + "dependency").Count(e => (string)e.Attribute("ref") == bomRef);
            if (count != 0)
            {
                throw new XunitException($"Expected no <dependency ref=\"{bomRef}\"> elements but found {count}.");
            }
        }

        private static int CountComponents(string xml, string bomRef)
        {
            var (document, ns) = Parse(xml);
            return document.Descendants(ns + "component").Count(c => (string)c.Attribute("bom-ref") == bomRef);
        }

        private static int CountEdges(string xml, string parentBomRef, string childBomRef)
        {
            var (document, ns) = Parse(xml);
            var entries = document.Root.Element(ns + "dependencies")?.Elements(ns + "dependency")
                ?? Enumerable.Empty<XElement>();
            return entries
                .Where(e => (string)e.Attribute("ref") == parentBomRef)
                .Elements(ns + "dependency")
                .Count(e => (string)e.Attribute("ref") == childBomRef);
        }

        private static (XDocument Document, XNamespace Ns) Parse(string xml)
        {
            var document = XDocument.Parse(xml);
            if (document.Root == null)
            {
                throw new XunitException("XML document has no root element.");
            }

            return (document, document.Root.Name.Namespace);
        }
    }
}
