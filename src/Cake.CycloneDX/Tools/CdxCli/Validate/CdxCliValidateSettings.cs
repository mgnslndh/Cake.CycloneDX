namespace Cake.CycloneDX.Tools.CdxCli.Validate;

/// <summary>
/// Contains settings used by the <c>CdxCliValidate</c> aliases.
/// </summary>
public class CdxCliValidateSettings : CdxCliSettings
{
    /// <summary>
    /// Gets or sets the format of the BOM to validate. When <see langword="null"/>, the CLI detects it.
    /// </summary>
    public CdxCliValidateInputFormat? InputFormat { get; set; }

    /// <summary>
    /// Gets or sets the CycloneDX specification version to validate against. When <see langword="null"/>, the CLI's default version is used.
    /// </summary>
    public CdxCliSpecificationVersion? InputVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the CLI exits with a non-zero exit code when the BOM is invalid.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>, in which case validation errors are only reported in the output and the build continues.
    /// Set it to <see langword="true"/> to make an invalid BOM fail the build.
    /// </remarks>
    public bool FailOnErrors { get; set; } = false;
}