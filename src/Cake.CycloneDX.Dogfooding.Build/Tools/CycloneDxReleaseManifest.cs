using System.Runtime.InteropServices;
using Cake.Core;

namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Describes a single CycloneDX CLI asset for a specific platform.
/// </summary>
/// <param name="Filename">The asset filename as published on GitHub releases.</param>
/// <param name="Sha256">The expected lowercase hex SHA-256 hash, or <see langword="null"/> to skip verification.</param>
internal sealed record CycloneDxCliAsset(string Filename, string? Sha256);

/// <summary>
/// Maps platform identifiers to their corresponding CycloneDX CLI assets for one release version.
/// </summary>
internal sealed class CycloneDxReleaseManifest
{
    private readonly Dictionary<(PlatformFamily, Architecture), CycloneDxCliAsset> _assets = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CycloneDxReleaseManifest"/> for the given release tag.
    /// </summary>
    /// <param name="version">The GitHub release tag, e.g. "v0.30.0".</param>
    public CycloneDxReleaseManifest(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Version = version;
    }

    /// <summary>Gets the release tag this manifest describes.</summary>
    public string Version { get; }

    /// <summary>
    /// Registers an asset for the given platform and architecture. Fluent.
    /// </summary>
    /// <param name="family">The OS platform family.</param>
    /// <param name="architecture">The processor architecture.</param>
    /// <param name="filename">The asset filename as published on GitHub releases.</param>
    /// <param name="sha256">The expected lowercase hex SHA-256 hash, or <see langword="null"/> to skip verification.</param>
    /// <returns>This manifest (for chaining).</returns>
    public CycloneDxReleaseManifest Add(
        PlatformFamily family,
        Architecture architecture,
        string filename,
        string? sha256 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        _assets[(family, architecture)] = new CycloneDxCliAsset(filename, sha256);
        return this;
    }

    /// <summary>
    /// Resolves the asset for the given platform and architecture, or <see langword="null"/> if not registered.
    /// </summary>
    /// <param name="family">The OS platform family.</param>
    /// <param name="architecture">The processor architecture.</param>
    /// <returns>The matching <see cref="CycloneDxCliAsset"/>, or <see langword="null"/>.</returns>
    public CycloneDxCliAsset? Resolve(PlatformFamily family, Architecture architecture) =>
        _assets.TryGetValue((family, architecture), out var asset) ? asset : null;
}
