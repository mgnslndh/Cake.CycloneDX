namespace Cake.CycloneDX.Tools.CdxCli.Merge;

/// <summary>
/// A BOM file format supported by the CycloneDX CLI <c>merge</c> command.
/// </summary>
public enum CdxCliMergeFormat
{
    /// <summary>Detect the format from the file extension.</summary>
    AutoDetect,

    /// <summary>CycloneDX JSON.</summary>
    Json,

    /// <summary>CycloneDX Protocol Buffers.</summary>
    Protobuf,

    /// <summary>CycloneDX XML.</summary>
    Xml
}