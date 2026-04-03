using System.Runtime.InteropServices;
using Cake.Core;

namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Registry of known CycloneDX CLI release manifests. Pre-registers a default manifest for
/// version v0.30.0 and allows callers to register additional versions via <see cref="Add"/>.
/// </summary>
internal sealed class CycloneDxReleaseManifestResolver
{
    private readonly Dictionary<string, CycloneDxReleaseManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="CycloneDxReleaseManifestResolver"/> class.
    /// </summary>
    public CycloneDxReleaseManifestResolver()
    {
        var v0300 = new CycloneDxReleaseManifest("v0.30.0")
            .Add(PlatformFamily.Windows, Architecture.X64, "cyclonedx-win-x64.exe", "1f563ba9644d2f2966fc8029fd701ca4af4f388d44c017c1d60559a1ecc9114f")
            .Add(PlatformFamily.Windows, Architecture.X86, "cyclonedx-win-x86.exe", "8eab8678920cd2688b717b2d8b784374bd6758f948d5ef2b3f5828def51b6fa2")
            .Add(PlatformFamily.Linux, Architecture.X64, "cyclonedx-linux-x64", "f89876326620f5fc78a9b27cc1af57d6ed13d019aab87490e1246a44a910babb")
            .Add(PlatformFamily.OSX, Architecture.X64, "cyclonedx-osx-x64", "1603264fd2968b8d617e48aa7e9cf17bee1d25a8ffe717aec37caf1605a21961")
            .Add(PlatformFamily.OSX, Architecture.Arm64, "cyclonedx-osx-arm64", "dabbaf07e543e7996f708147475e2daa69ddf8a8683c5b06febc7d3f074e5e24");

        _manifests[v0300.Version] = v0300;
    }

    /// <summary>
    /// Registers a manifest, replacing any existing entry for the same version.
    /// </summary>
    /// <param name="manifest">The manifest to register.</param>
    /// <returns>This resolver (for chaining).</returns>
    public CycloneDxReleaseManifestResolver Add(CycloneDxReleaseManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        _manifests[manifest.Version] = manifest;
        return this;
    }

    /// <summary>
    /// Resolves the asset for the given version, platform family and architecture.
    /// </summary>
    /// <param name="version">The GitHub release tag, e.g. "v0.30.0".</param>
    /// <param name="family">The OS platform family.</param>
    /// <param name="architecture">The processor architecture.</param>
    /// <returns>The matching <see cref="CycloneDxCliAsset"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no manifest is registered for <paramref name="version"/>, or when the
    /// manifest has no entry for the given <paramref name="family"/> / <paramref name="architecture"/> combination.
    /// </exception>
    public CycloneDxCliAsset ResolveAsset(string version, PlatformFamily family, Architecture architecture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (!_manifests.TryGetValue(version, out var manifest))
        {
            throw new InvalidOperationException(
                $"No manifest registered for CycloneDX CLI version '{version}'. " +
                $"Register one via {nameof(Add)} before downloading.");
        }

        return manifest.Resolve(family, architecture)
            ?? throw new InvalidOperationException(
                $"Manifest for version '{version}' has no asset defined for {family}/{architecture}.");
    }

    /// <summary>
    /// Resolves the asset filename for the given version, platform family and architecture.
    /// </summary>
    /// <param name="version">The GitHub release tag, e.g. "v0.30.0".</param>
    /// <param name="family">The OS platform family.</param>
    /// <param name="architecture">The processor architecture.</param>
    /// <returns>The platform-specific asset filename.</returns>
    public string ResolveFilename(string version, PlatformFamily family, Architecture architecture) =>
        ResolveAsset(version, family, architecture).Filename;
}
