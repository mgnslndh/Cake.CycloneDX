using System.Diagnostics.CodeAnalysis;

namespace Cake.CycloneDX.Tools.CdxCli;

/// <summary>
/// A CycloneDX specification version, as passed to the CycloneDX CLI.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum CdxCliSpecificationVersion
{
    /// <summary>CycloneDX specification version 1.0.</summary>
    V1_0,

    /// <summary>CycloneDX specification version 1.1.</summary>
    V1_1,

    /// <summary>CycloneDX specification version 1.2.</summary>
    V1_2,

    /// <summary>CycloneDX specification version 1.3.</summary>
    V1_3,

    /// <summary>CycloneDX specification version 1.4.</summary>
    V1_4,

    /// <summary>CycloneDX specification version 1.5.</summary>
    V1_5,

    /// <summary>CycloneDX specification version 1.6.</summary>
    V1_6,

    /// <summary>CycloneDX specification version 1.7.</summary>
    V1_7,
}