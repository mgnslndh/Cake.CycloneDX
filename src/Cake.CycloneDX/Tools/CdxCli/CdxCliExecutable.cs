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
    /// <param name="is64Bit">Whether the platform is 64-bit.</param>
    /// <returns>The platform-specific executable filename.</returns>
    /// <exception cref="CakeException">Thrown when the platform is not supported.</exception>
    public static string GetFilename(PlatformFamily family, bool is64Bit) =>
        (family, is64Bit) switch
        {
            (PlatformFamily.Windows, true) => "cyclonedx-win-x64.exe",
            (PlatformFamily.Windows, false) => "cyclonedx-win-x86.exe",
            (PlatformFamily.Linux, true) => "cyclonedx-linux-x64",
            (PlatformFamily.OSX, true) => "cyclonedx-osx-x64",
            _ => throw new CakeException("Unsupported platform.")
        };
}
