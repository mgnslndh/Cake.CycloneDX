using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxDotNet;

public class CdxDotNetSettings : ToolSettings
{
    public string? Framework { get; set; }
    public string? Runtime { get; set; }
    public DirectoryPath? Output { get; set; }
    public string? FileName { get; set; }
    public bool ExcludeDevelopmentDependencies { get; set; } = false;
    public bool ExcludeTestProjects { get; set; } = false;
    public bool Recursive { get; set; }
    public bool DisablePackageRestore { get; set; }
    public bool IncludeProjectReferences { get; set; } = false;

    public string? ComponentName { get; set; }
    public string? ComponentVersion { get; set; }
    public CdxComponentClassification? ComponentType { get; set; }
    public ExcludeFilterHashSet ExcludeFilters { get; set; } = new();
    public CdxDotNetSpecificationVersion? SpecVersion { get; set; }
    public CdxDotNetOutputFormat? OutputFormat { get; set; }
}