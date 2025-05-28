using System.Diagnostics.CodeAnalysis;

namespace Cake.CycloneDX.Tools.CdxDotNet;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum CdxComponentClassification
{
    Application,
    Framework,
    Library,
    Operating_System,
    Device,
    File,
    Container,
    Firmware,
    Device_Driver,
    Platform,
    Machine_Learning_Model,
    Data,
    Cryptographic_Asset,
    Null
}