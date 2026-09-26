using System.Xml;
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.CycloneDX.Tools.CdxRefine;

/// <summary>
/// Contains functionality for shaping the component set and dependency graph of a CycloneDX XML BOM, typically one
/// produced by <c>CdxDotNet</c> and merged with <c>CdxCliMerge</c>.
/// </summary>
/// <remarks>
/// <para>The steps run in this order, and a step without settings is skipped:</para>
/// <list type="number">
/// <item><description><b>Adopt</b> orphaned components into the dependency tree (<see cref="CdxRefineSettings.Adoptions"/>, <see cref="CdxRefineSettings.AdoptOrphanedComponents"/>).</description></item>
/// <item><description><b>Exclude</b> matching components (<see cref="CdxRefineSettings.Exclusions"/>).</description></item>
/// <item><description><b>Remove</b> components that are unreachable from the metadata component (<see cref="CdxRefineSettings.RemoveOrphanedComponents"/>).</description></item>
/// <item><description><b>Group</b> and then <b>type</b> the remaining components (<see cref="CdxRefineSettings.GroupSettings"/>, <see cref="CdxRefineSettings.TypeSettings"/>).</description></item>
/// </list>
/// <para>Only the XML format is supported.</para>
/// </remarks>
[CakeAliasCategory("CycloneDX")]
public static class CdxRefineAliases
{
    /// <summary>
    /// Refines a CycloneDX BOM given as an XML string.
    /// </summary>
    /// <example>
    /// <code>
    /// var xml = System.IO.File.ReadAllText("./artifacts/bom.xml");
    /// var refined = CdxRefine(xml, new CdxRefineSettings().WithExcludeByName("^xunit"));
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="xml">The BOM as CycloneDX XML.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to leave the BOM unchanged.</param>
    /// <returns>The refined BOM as CycloneDX XML.</returns>
    /// <exception cref="System.Xml.XmlException"><paramref name="xml"/> is not well-formed XML.</exception>
    /// <exception cref="InvalidOperationException">The BOM has no root element, or a component to group or type has no name or type.</exception>
    /// <exception cref="CakeException">An adoption or orphan removal cannot be performed; the message explains why.</exception>
    [CakeMethodAlias]
    public static string CdxRefine(this ICakeContext context, string xml, CdxRefineSettings? settings = null)
    {
        var document = XDocument.Parse(xml, LoadOptions.SetLineInfo);
        CdxRefine(context, document, settings);
        return document.ToString(SaveOptions.None);
    }

    /// <summary>
    /// Refines a CycloneDX XML BOM file and writes the result to another file.
    /// </summary>
    /// <remarks>
    /// The output directory is created if it does not exist. The input and output paths may be the same file.
    /// </remarks>
    /// <example>
    /// <code>
    /// var settings = new CdxRefineSettings()
    ///     .WithAdoptOrphanedComponents()
    ///     .WithExcludeByName(@"^xunit(\..+)?$")
    ///     .WithRemoveOrphanedComponents()
    ///     .WithGroupByName("Test tools", "^xunit");
    ///
    /// CdxRefine("./artifacts/merged.xml", "./artifacts/bom.xml", settings);
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="inputPath">The BOM file to read.</param>
    /// <param name="outputPath">The file to write the refined BOM to.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to leave the BOM unchanged.</param>
    /// <exception cref="ArgumentException"><paramref name="inputPath"/> or <paramref name="outputPath"/> is empty.</exception>
    /// <exception cref="CakeException">
    /// The input file does not exist, or an adoption or orphan removal cannot be performed; the message explains why.
    /// </exception>
    /// <exception cref="System.Xml.XmlException">The input file is not well-formed XML.</exception>
    /// <exception cref="InvalidOperationException">The BOM has no root element, or a component to group or type has no name or type.</exception>
    [CakeMethodAlias]
    public static void CdxRefine(this ICakeContext context, FilePath inputPath, FilePath outputPath, CdxRefineSettings? settings = null)
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

        CdxRefine(context, document, settings);

        var outputDir = context.FileSystem.GetDirectory(outputPath.GetDirectory());
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        using var writeStream = context.FileSystem.GetFile(outputPath).OpenWrite();
        document.Save(writeStream);
    }

    /// <summary>
    /// Refines a CycloneDX XML BOM, modifying the document in place.
    /// </summary>
    /// <example>
    /// <code>
    /// var document = System.Xml.Linq.XDocument.Load("./artifacts/bom.xml");
    /// CdxRefine(document, new CdxRefineSettings().WithTypeByName("framework", "^Microsoft\\.AspNetCore\\.App$"));
    /// document.Save("./artifacts/bom.xml");
    /// </code>
    /// </example>
    /// <param name="context">The context.</param>
    /// <param name="document">The BOM to refine.</param>
    /// <param name="settings">The settings, or <see langword="null"/> to leave the BOM unchanged.</param>
    /// <exception cref="InvalidOperationException">The BOM has no root element, or a component to group or type has no name or type.</exception>
    /// <exception cref="CakeException">An adoption or orphan removal cannot be performed; the message explains why.</exception>
    [CakeMethodAlias]
    public static void CdxRefine(this ICakeContext context, XDocument document, CdxRefineSettings? settings = null)
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

        settings ??= new CdxRefineSettings();

        var graph = new CdxDependencyGraph(document, ns);

        if (settings.Adoptions.Count > 0 || settings.AdoptOrphanedComponents)
        {
            ComponentAdopter.Adopt(context, graph, settings.Adoptions, settings.AdoptOrphanedComponents);
        }

        if (settings.Exclusions.Count > 0)
        {
            ComponentExcluder.Exclude(context, graph, settings.Exclusions);
        }

        if (settings.RemoveOrphanedComponents)
        {
            OrphanedComponentRemover.Remove(context, graph);
        }

        if (settings.GroupSettings.Any())
        {
            RefineComponentGroups(context, document, ns, settings.GroupSettings);
        }

        if (settings.TypeSettings.Any())
        {
            RefineComponentTypes(context, document, ns, settings.TypeSettings);
        }
    }

    private static void RefineComponentGroups(ICakeContext context, XDocument document, XNamespace ns, IEnumerable<CdxRefineGroupSettings> settings)
    {
        XElement? metadataComponentElement = GetMetadata(document, ns);
        var componentsParent = document.Root?.Element(ns + "components");

        foreach (var groupSettings in settings)
        {
            if (metadataComponentElement != null && groupSettings.Criteria.IsMatch(metadataComponentElement))
            {
                AssignGroup(context, metadataComponentElement, ns, groupSettings.Group);
            }

            if (componentsParent != null)
            {
                var matchedComponents = componentsParent.Elements(ns + "component")
                    .Where(componentElement => groupSettings.Criteria.IsMatch(componentElement))
                    .ToList();

                foreach (var matchedComponent in matchedComponents)
                {
                    AssignGroup(context, matchedComponent, ns, groupSettings.Group);
                }
            }
        }
    }

    private static XElement? GetMetadata(XDocument document, XNamespace ns)
    {
        var metadataElement = document.Descendants(ns + "metadata").FirstOrDefault();
        return metadataElement?.Element(ns + "component");
    }

    private static void RefineComponentTypes(ICakeContext context, XDocument document, XNamespace ns, IEnumerable<CdxRefineTypeSettings> settings)
    {
        XElement? metadataComponentElement = GetMetadata(document, ns);
        var componentsParent = document.Root?.Element(ns + "components");

        foreach (var typeSettings in settings)
        {
            if (metadataComponentElement != null && typeSettings.Criteria.IsMatch(metadataComponentElement))
            {
                AssignType(context, metadataComponentElement, ns, typeSettings.Type);
            }

            if (componentsParent != null)
            {
                var matchedComponents = componentsParent.Elements(ns + "component")
                    .Where(componentElement => typeSettings.Criteria.IsMatch(componentElement))
                    .ToList();

                foreach (var matchedComponent in matchedComponents)
                {
                    AssignType(context, matchedComponent, ns, typeSettings.Type);
                }
            }
        }
    }

    private static void AssignGroup(ICakeContext context, XElement component, XNamespace ns, string groupName)
    {
        var componentNameElement = component.Element(ns + "name");

        if (componentNameElement == null)
        {
            throw new InvalidOperationException($"Component at line {((IXmlLineInfo)component).LineNumber} is missing a <name> element.");
        }

        var componentName = componentNameElement.Value;

        context.Log.Information(Verbosity.Verbose, "Assigning group '{0}' to component '{1}'", groupName, componentName);

        XElement? groupElement = component.Element(ns + "group");

        groupElement?.Remove();
        groupElement = new XElement(ns + "group");
        componentNameElement.AddBeforeSelf(groupElement);

        groupElement.Value = groupName;
    }

    private static void AssignType(ICakeContext context, XElement component, XNamespace ns, string typeName)
    {
        var componentTypeAttribute = component.Attribute("type");

        if (componentTypeAttribute == null)
        {
            throw new InvalidOperationException($"Component at line {((IXmlLineInfo)component).LineNumber} is missing a type attribute.");
        }

        var componentType = componentTypeAttribute.Value;

        var componentNameElement = component.Element(ns + "name");

        if (componentNameElement == null)
        {
            throw new InvalidOperationException($"Component at line {((IXmlLineInfo)component).LineNumber} is missing a <name> element.");
        }

        var componentName = componentNameElement.Value;

        context.Log.Information(Verbosity.Verbose, "Changing component type for '{0}' from '{1}' to '{2}'", componentName, componentType, typeName);

        componentTypeAttribute.Value = typeName;
    }
}