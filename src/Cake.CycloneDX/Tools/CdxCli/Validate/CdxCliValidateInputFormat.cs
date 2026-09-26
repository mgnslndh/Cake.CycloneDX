namespace Cake.CycloneDX.Tools.CdxCli.Validate;

/// <summary>
/// A BOM file format supported by the CycloneDX CLI <c>validate</c> command.
/// </summary>
public enum CdxCliValidateInputFormat
{
    /// <summary>Detect the format from the file extension.</summary>
    AutoDetect,

    /// <summary>CycloneDX JSON.</summary>
    Json,

    /// <summary>CycloneDX XML.</summary>
    Xml
}