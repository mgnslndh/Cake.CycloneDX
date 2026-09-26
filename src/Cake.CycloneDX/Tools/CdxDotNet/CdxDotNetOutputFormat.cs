namespace Cake.CycloneDX.Tools.CdxDotNet;

/// <summary>
/// The file format of a BOM written by the CycloneDX .NET tool.
/// </summary>
public enum CdxDotNetOutputFormat
{
    /// <summary>Let the tool choose the format. This is the tool's default.</summary>
    Auto,

    /// <summary>CycloneDX JSON.</summary>
    Json,

    /// <summary>CycloneDX JSON with relaxed character escaping.</summary>
    UnsafeJson,

    /// <summary>CycloneDX XML.</summary>
    Xml,
}
