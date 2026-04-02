namespace Cake.CycloneDX.Tools.CdxCli.Merge;

public class CdxCliMergeSettings : CdxCliSettings
{
    public CdxCliMergeFormat? InputFormat { get; set; }
    public CdxCliMergeFormat? OutputFormat { get; set; }
    public CdxCliSpecificationVersion? OutputVersion { get; set; }
    public bool Hierarchical { get; set; }
    public string? Group { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
}