using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.CycloneDX.Tools.CdxDeduplicate;

/// <summary>
/// Contains functionality for removing duplicate components from a CycloneDX XML BOM, for example after merging
/// BOMs with <c>CdxCliMerge</c>.
/// </summary>
/// <remarks>
/// <para>
/// Only the top-level components of <c>/bom/components</c> are compared. When several components share a key, the first
/// one is kept and the others are removed, and a warning is logged for each such group. Components that have no key
/// are kept but moved after the deduplicated components.
/// </para>
/// <para>
/// When deduplicating by purl, <c>&lt;dependency ref="..."&gt;</c> elements that pointed to a removed component's
/// bom-ref are redirected to the bom-ref of the component that was kept.
/// </para>
/// <para>Only the XML format is supported.</para>
/// </remarks>
[CakeAliasCategory("CycloneDX")]
public static class CdxDeduplicateAliases
{
    /// <summary>
    /// Removes duplicate components from a CycloneDX BOM given as an XML string.
    /// </summary>
    /// <example>
    /// <code>
    /// var xml = System.IO.File.ReadAllText("./artifacts/bom.xml");
    /// var deduplicated = CdxDeduplicate(xml);
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="xml">The BOM as CycloneDX XML.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to deduplicate by both bom-ref and purl.</param>
    /// <returns>The deduplicated BOM as CycloneDX XML.</returns>
    /// <exception cref="System.Xml.XmlException"><paramref name="xml"/> is not well-formed XML.</exception>
    /// <exception cref="InvalidOperationException">The BOM has no root element.</exception>
    [CakeMethodAlias]
    public static string CdxDeduplicate(this ICakeContext context, string xml, CdxDeduplicateSettings? settings = null)
    {
        var document = XDocument.Parse(xml, LoadOptions.SetLineInfo);
        CdxDeduplicate(context, document, settings);
        return document.ToString(SaveOptions.None);
    }

    /// <summary>
    /// Removes duplicate components from a CycloneDX XML BOM file and writes the result to another file.
    /// </summary>
    /// <remarks>
    /// The output directory is created if it does not exist. The input and output paths may be the same file.
    /// </remarks>
    /// <example>
    /// <code>
    /// CdxDeduplicate(
    ///     "./artifacts/merged.xml",
    ///     "./artifacts/bom.xml",
    ///     new CdxDeduplicateSettings { DeduplicateByBomRef = false });
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="inputPath">The BOM file to read.</param>
    /// <param name="outputPath">The file to write the deduplicated BOM to.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to deduplicate by both bom-ref and purl.</param>
    /// <exception cref="ArgumentException"><paramref name="inputPath"/> or <paramref name="outputPath"/> is empty.</exception>
    /// <exception cref="CakeException">The input file does not exist.</exception>
    /// <exception cref="System.Xml.XmlException">The input file is not well-formed XML.</exception>
    /// <exception cref="InvalidOperationException">The BOM has no root element.</exception>
    [CakeMethodAlias]
    public static void CdxDeduplicate(this ICakeContext context, FilePath inputPath, FilePath outputPath, CdxDeduplicateSettings? settings = null)
    {
        Throw.IfFullPathIsNullOrWhitespace(inputPath);
        Throw.IfFullPathIsNullOrWhitespace(outputPath);

        var inputFile = context.FileSystem.GetFile(inputPath);
        if (!inputFile.Exists)
        {
            throw new CakeException($"Input file '{inputPath.FullPath}' does not exist.");
        }

        XDocument document;
        using (var readStream = inputFile.OpenRead())
        {
            document = XDocument.Load(readStream, LoadOptions.SetLineInfo);
        }

        CdxDeduplicate(context, document, settings);

        var outputDir = context.FileSystem.GetDirectory(outputPath.GetDirectory());
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        using var writeStream = context.FileSystem.GetFile(outputPath).OpenWrite();
        document.Save(writeStream);
    }

    /// <summary>
    /// Removes duplicate components from a CycloneDX XML BOM, modifying the document in place.
    /// </summary>
    /// <example>
    /// <code>
    /// var document = System.Xml.Linq.XDocument.Load("./artifacts/bom.xml");
    /// CdxDeduplicate(document);
    /// document.Save("./artifacts/bom.xml");
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="document">The BOM to deduplicate.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to deduplicate by both bom-ref and purl.</param>
    /// <exception cref="InvalidOperationException">The BOM has no root element.</exception>
    [CakeMethodAlias]
    public static void CdxDeduplicate(this ICakeContext context, XDocument document, CdxDeduplicateSettings? settings = null)
    {
        settings ??= new CdxDeduplicateSettings();

        if (settings.DeduplicateByBomRef)
        {
            DeduplicateComponentsByBomRef(context, document);
        }

        if (settings.DeduplicateByPurl)
        {
            DeduplicateComponentsByPurl(context, document);
        }
    }

    private static void DeduplicateComponentsByPurl(ICakeContext context, XDocument document)
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

        var componentsParent = document.Root?.Element(ns + "components");
        if (componentsParent == null)
        {
            return;
        }

        var skipped = componentsParent.Elements(ns + "component")
            .Where(c => c.Element(ns + "purl") == null)
            .ToList();

        var groups = componentsParent.Elements(ns + "component")
            .Where(c => c.Element(ns + "purl") != null)
            .GroupBy(c => new
            {
                Purl = c.Element(ns + "purl")!
                         .Value
                         .Trim()
                         .ToLowerInvariant()
            })
            .ToList();

        foreach (var group in groups)
        {
            if (group.Count() > 1)
            {
                context.Log.Warning("Deduplicated {0} components with purl '{1}'", group.Count(), group.Key.Purl);
            }
        }

        // Build a redirect map: discarded bom-ref → retained bom-ref.
        // Needed to fix up <dependency> references that would otherwise point to removed components.
        var dependencyRedirects = new Dictionary<string, string>();
        foreach (var group in groups.Where(g => g.Count() > 1))
        {
            var retainedBomRef = group.First().Attribute("bom-ref")?.Value;
            if (retainedBomRef == null)
            {
                continue;
            }

            foreach (var discarded in group.Skip(1))
            {
                var discardedBomRef = discarded.Attribute("bom-ref")?.Value;
                if (discardedBomRef != null && discardedBomRef != retainedBomRef)
                {
                    dependencyRedirects[discardedBomRef] = retainedBomRef;
                }
            }
        }

        var deduplicated = groups
            .Select(g => g.First());

        componentsParent.RemoveNodes(); // Clear existing components

        foreach (var component in deduplicated)
        {
            componentsParent.Add(component);
        }

        foreach (var component in skipped)
        {
            componentsParent.Add(component); // Re-add components without PURL
        }

        if (dependencyRedirects.Count > 0)
        {
            RewriteDependencyRefs(document, ns, dependencyRedirects);
        }
    }

    private static void DeduplicateComponentsByBomRef(ICakeContext context, XDocument document)
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

        var componentsParent = document.Root?.Element(ns + "components");
        if (componentsParent == null)
        {
            return;
        }

        var skipped = componentsParent.Elements(ns + "component")
            .Where(c => c.Attribute("bom-ref") == null)
            .ToList();

        var groups = componentsParent.Elements(ns + "component")
            .Where(c => c.Attribute("bom-ref") != null)
            .GroupBy(c => new
            {
                BomRef = c.Attribute("bom-ref")?.Value
            })
            .ToList();

        foreach (var group in groups)
        {
            if (group.Count() > 1)
            {
                context.Log.Warning("Deduplicated {0} components with bom-ref '{1}'", group.Count(), group.Key.BomRef);
            }
        }

        var deduplicated = groups
            .Select(g => g.First());

        componentsParent.RemoveNodes(); // Clear existing components

        foreach (var component in deduplicated)
        {
            componentsParent.Add(component);
        }

        foreach (var component in skipped)
        {
            componentsParent.Add(component);
        }
    }

    // No dependency rewriting needed here: all duplicates in a bom-ref group share the same
    // bom-ref value, so any <dependency ref="..."> already points to the value the retained
    // component keeps.
    private static void RewriteDependencyRefs(XDocument document, XNamespace ns, Dictionary<string, string> redirects)
    {
        foreach (var dependency in document.Descendants(ns + "dependency"))
        {
            var refAttr = dependency.Attribute("ref");
            if (refAttr != null && redirects.TryGetValue(refAttr.Value, out var newRef))
            {
                refAttr.Value = newRef;
            }
        }
    }
}

/// <summary>
/// Contains settings used by the <c>CdxDeduplicate</c> aliases.
/// </summary>
public class CdxDeduplicateSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether components with the same <c>bom-ref</c> attribute are deduplicated.
    /// Defaults to <see langword="true"/>. This step runs first.
    /// </summary>
    public bool DeduplicateByBomRef { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether components with the same <c>purl</c> are deduplicated.
    /// Defaults to <see langword="true"/>. Purls are compared case-insensitively, ignoring leading and trailing white space.
    /// </summary>
    public bool DeduplicateByPurl { get; set; } = true;
}