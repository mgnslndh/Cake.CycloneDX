using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxDotNet;

/// <summary>
/// Contains settings used by the <c>CdxDotNet</c> aliases.
/// </summary>
/// <remarks>
/// Each property maps to a CycloneDX .NET tool option. Properties left at their default value are not passed to the
/// tool, so the tool's own default applies. <see cref="CdxDotNetSettingsExtensions"/> provides fluent setters.
/// </remarks>
public class CdxDotNetSettings : ToolSettings
{
    /// <summary>
    /// Gets or sets the target framework to analyze, such as <c>net8.0</c>. When <see langword="null"/>, all target frameworks are aggregated.
    /// </summary>
    public string? Framework { get; set; }

    /// <summary>
    /// Gets or sets the runtime identifier to analyze, such as <c>linux-x64</c>. When <see langword="null"/>, all runtimes are aggregated.
    /// </summary>
    public string? Runtime { get; set; }

    /// <summary>
    /// Gets or sets the directory to write the BOM to.
    /// </summary>
    public DirectoryPath? Output { get; set; }

    /// <summary>
    /// Gets or sets the file name of the BOM. When <see langword="null"/>, the tool uses <c>bom.xml</c> or <c>bom.json</c>.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude development dependencies, such as analyzers, from the BOM.
    /// </summary>
    public bool ExcludeDevelopmentDependencies { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to exclude test projects from the BOM.
    /// </summary>
    public bool ExcludeTestProjects { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to also scan the project references of a single project file, recursively.
    /// </summary>
    public bool Recursive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip the package restore the tool performs before analyzing.
    /// </summary>
    public bool DisablePackageRestore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include project references as components. Only applies to project files.
    /// </summary>
    public bool IncludeProjectReferences { get; set; } = false;

    /// <summary>
    /// Gets or sets the name of the BOM metadata component, overriding the name the tool generates.
    /// </summary>
    public string? ComponentName { get; set; }

    /// <summary>
    /// Gets or sets the version of the BOM metadata component. When <see langword="null"/>, the tool uses <c>0.0.0</c>.
    /// </summary>
    public string? ComponentVersion { get; set; }

    /// <summary>
    /// Gets or sets the type of the BOM metadata component. When <see langword="null"/>, the tool uses
    /// <see cref="CdxComponentClassification.Application"/>.
    /// </summary>
    public CdxComponentClassification? ComponentType { get; set; }

    /// <summary>
    /// Gets or sets the packages to exclude from the BOM. Their transitive dependencies are excluded too.
    /// </summary>
    public ExcludeFilterHashSet ExcludeFilters { get; set; } = new();

    /// <summary>
    /// Gets or sets the CycloneDX specification version of the BOM. When <see langword="null"/>, the tool's default version is used.
    /// </summary>
    public CdxDotNetSpecificationVersion? SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the file format of the BOM. When <see langword="null"/>, the tool's default, <see cref="CdxDotNetOutputFormat.Auto"/>, is used.
    /// </summary>
    public CdxDotNetOutputFormat? OutputFormat { get; set; }
}