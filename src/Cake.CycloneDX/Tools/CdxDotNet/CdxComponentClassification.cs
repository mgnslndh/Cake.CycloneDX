using System.Diagnostics.CodeAnalysis;

namespace Cake.CycloneDX.Tools.CdxDotNet;

/// <summary>
/// A CycloneDX component type, used to set the type of the BOM metadata component.
/// </summary>
/// <remarks>
/// The member names match the values accepted by the CycloneDX .NET tool's <c>--set-type</c> option.
/// </remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum CdxComponentClassification
{
    /// <summary>A software application. This is the CycloneDX .NET tool's default.</summary>
    Application,

    /// <summary>A software framework.</summary>
    Framework,

    /// <summary>A software library.</summary>
    Library,

    /// <summary>An operating system.</summary>
    Operating_System,

    /// <summary>A hardware device.</summary>
    Device,

    /// <summary>A file.</summary>
    File,

    /// <summary>A container image.</summary>
    Container,

    /// <summary>Device firmware.</summary>
    Firmware,

    /// <summary>A device driver.</summary>
    Device_Driver,

    /// <summary>A platform, such as a runtime environment or a cloud service.</summary>
    Platform,

    /// <summary>A machine learning model.</summary>
    Machine_Learning_Model,

    /// <summary>A collection of data.</summary>
    Data,

    /// <summary>A cryptographic asset.</summary>
    Cryptographic_Asset,

    /// <summary>No component type.</summary>
    Null
}