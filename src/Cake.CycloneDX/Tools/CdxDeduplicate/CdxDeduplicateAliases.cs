using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.CycloneDX.Tools.CdxDeduplicate;

[CakeAliasCategory("CycloneDX")]
public static class CdxDeduplicateAliases
{
    [CakeMethodAlias]
    public static void CdxDeduplicate(this ICakeContext context, FilePath inputPath, FilePath outputPath, CdxDeduplicateSettings? settings = null)
    {
        var document = XDocument.Load(inputPath.FullPath);
        DeduplicateComponents(context, document);
        document.Save(outputPath.FullPath);
    }

    private static void DeduplicateComponents(ICakeContext context, XDocument document)
    {
        XElement? root = document.Root;
        if (root is null)
        {
            throw new InvalidOperationException("SBOM does not include a root element.");
        }

        XNamespace? ns = document.Root?.GetDefaultNamespace();
        if (ns == null)
        {
            throw new InvalidOperationException("SBOM does not contain a default namespace on the root element.");
        }

        var componentsParent = document.Descendants(ns + "components").FirstOrDefault();
        if (componentsParent == null)
        {
            return;
        }

        var groups = componentsParent.Elements(ns + "component")
            .GroupBy(c => new
            {
                Purl = c.Element(ns + "purl")?.Value
            })
            .ToList();

        foreach (var group in groups)
        {
            if (group.Count() > 1)
            {
                context.Log.Warning("Deduplicated {0}", group.Key.Purl);
            }
        }

        var deduplicated = groups
            .Select(g => g.First());

        componentsParent.RemoveNodes(); // Clear existing components
        foreach (var component in deduplicated)
        {
            componentsParent.Add(component);
        }
    }
}

public class CdxDeduplicateSettings
{
}