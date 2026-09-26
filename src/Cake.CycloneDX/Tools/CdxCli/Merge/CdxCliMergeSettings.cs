namespace Cake.CycloneDX.Tools.CdxCli.Merge;

/// <summary>
/// Contains settings used by <see cref="CdxCliAliases.CdxCliMerge"/>.
/// </summary>
public class CdxCliMergeSettings : CdxCliSettings
{
    /// <summary>
    /// Gets or sets the format of the input BOMs. When <see langword="null"/>, the CLI detects it.
    /// </summary>
    public CdxCliMergeFormat? InputFormat { get; set; }

    /// <summary>
    /// Gets or sets the format of the merged BOM. When <see langword="null"/>, the CLI derives it from the output file extension.
    /// </summary>
    public CdxCliMergeFormat? OutputFormat { get; set; }

    /// <summary>
    /// Gets or sets the CycloneDX specification version of the merged BOM. When <see langword="null"/>, the CLI's default version is used.
    /// </summary>
    public CdxCliSpecificationVersion? OutputVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform a hierarchical merge.
    /// </summary>
    /// <remarks>
    /// A hierarchical merge nests each input BOM's components under that BOM's metadata component, so every input
    /// BOM must describe its subject in a metadata component. It requires <see cref="Name"/> and <see cref="Version"/>,
    /// which describe the new metadata component of the merged BOM.
    /// </remarks>
    public bool Hierarchical { get; set; }

    /// <summary>
    /// Gets or sets the group of the software the merged BOM describes.
    /// </summary>
    /// <remarks>
    /// This value is not currently passed to the CycloneDX CLI.
    /// </remarks>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the name of the software the merged BOM describes. Required when <see cref="Hierarchical"/> is <see langword="true"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the software the merged BOM describes. Required when <see cref="Hierarchical"/> is <see langword="true"/>.
    /// </summary>
    public string? Version { get; set; }
}