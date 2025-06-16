using System.Xml;
using System.Xml.Linq;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.CycloneDX.Tools.CdxRefine;

[CakeAliasCategory("CycloneDX")]
public static class CdxRefineAliases
{
    [CakeMethodAlias]
    public static string CdxRefine(this ICakeContext context, string xml, CdxRefineSettings? settings = null)
    {
        var document = XDocument.Parse(xml, LoadOptions.SetLineInfo);
        CdxRefine(context, document, settings);
        return document.ToString(SaveOptions.None);
    }

    [CakeMethodAlias]
    public static void CdxRefine(this ICakeContext context, FilePath inputPath, FilePath outputPath, CdxRefineSettings? settings = null)
    {
        var document = XDocument.Load(inputPath.FullPath, LoadOptions.SetLineInfo);
        CdxRefine(context, document, settings);
        document.Save(outputPath.FullPath);
    }

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

        if (settings.GroupSettings.Any())
        {
            RefineComponentGroups(context, document, ns, settings.GroupSettings);
        }
    }

    private static void RefineComponentGroups(ICakeContext context, XDocument document, XNamespace ns, IEnumerable<CdxRefineGroupSettings> settings)
    {
        var componentsParent = document.Descendants(ns + "components").FirstOrDefault();
        if (componentsParent == null)
        {
            return;
        }

        foreach (var groupSettings in settings)
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

        if (groupElement == null)
        {
            groupElement = new XElement(ns + "group");
            componentNameElement.AddBeforeSelf(groupElement);
        }

        groupElement.Value = groupName;
    }
}