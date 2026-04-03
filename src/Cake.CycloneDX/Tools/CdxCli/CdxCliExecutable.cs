using System.Runtime.InteropServices;
using Cake.Core;

namespace Cake.CycloneDX.Tools.CdxCli;

/// <summary>
/// Resolves the platform-specific CycloneDX CLI executable filename,
/// matching the asset names published to GitHub releases.
/// </summary>
public static class CdxCliExecutable
{
    /// <summary>
    /// Returns the CycloneDX CLI executable filename for the given platform.
    /// The returned name matches the asset filename used in GitHub releases.
    /// </summary>
    /// <param name="family">The OS platform family.</param>
    /// <param name="architecture">The processor architecture.</param>
    /// <returns>The platform-specific executable filename.</returns>
    /// <exception cref="CakeException">Thrown when the platform is not supported.</exception>
    public static string GetFilename(PlatformFamily family, Architecture architecture) =>
        (family, architecture) switch
        {
            (PlatformFamily.Windows, Architecture.X64) => "cyclonedx-win-x64.exe",
            (PlatformFamily.Windows, Architecture.X86) => "cyclonedx-win-x86.exe",
            (PlatformFamily.Linux, Architecture.X64) => "cyclonedx-linux-x64",
            (PlatformFamily.OSX, Architecture.X64) => "cyclonedx-osx-x64",
            (PlatformFamily.OSX, Architecture.Arm64) => "cyclonedx-osx-arm64",
            _ => throw new CakeException("Unsupported platform.")
        };
}
