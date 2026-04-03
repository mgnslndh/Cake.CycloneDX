using System.Runtime.InteropServices;
using Cake.Common.Diagnostics;
using Cake.Core;

namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Ensures the CycloneDX CLI tool is available, downloading and verifying the correct
/// release asset from GitHub. The asset to download is determined by consulting a
/// <see cref="CycloneDxReleaseManifestResolver"/> for the requested version and platform.
/// </summary>
internal sealed class CycloneDxCliDownloader
{
    private const string Owner = "CycloneDX";
    private const string Repository = "cyclonedx-cli";

    private readonly CycloneDxReleaseManifestResolver _resolver;
    private readonly GitHubReleaseDownloader _downloader = new();

    /// <summary>
    /// Initializes a new instance of <see cref="CycloneDxCliDownloader"/> with the given
    /// manifest resolver.
    /// </summary>
    /// <param name="resolver">
    /// The resolver that maps release versions and platforms to the correct asset filename
    /// and SHA-256 hash.
    /// </param>
    public CycloneDxCliDownloader(CycloneDxReleaseManifestResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <summary>
    /// Downloads the CycloneDX CLI executable for the specified version, resolving the
    /// platform family and architecture from the current environment.
    /// </summary>
    /// <param name="context">The Cake context.</param>
    /// <param name="version">The release tag, e.g. "v0.30.0".</param>
    /// <param name="behavior">Controls when the file is downloaded. Defaults to <see cref="DownloadBehavior.IfNeeded"/>.</param>
    public void Download(
        ICakeContext context,
        string version,
        DownloadBehavior behavior = DownloadBehavior.IfNeeded)
    {
        Download(
            context,
            version,
            context.Environment.Platform.Family,
            RuntimeInformation.ProcessArchitecture,
            behavior);
    }

    /// <summary>
    /// Downloads the CycloneDX CLI executable for the specified version and platform,
    /// controlled by <paramref name="behavior"/>.
    /// </summary>
    /// <param name="context">The Cake context.</param>
    /// <param name="version">The release tag, e.g. "v0.30.0".</param>
    /// <param name="family">The OS platform family.</param>
    /// <param name="architecture">The processor architecture.</param>
    /// <param name="behavior">Controls when the file is downloaded. Defaults to <see cref="DownloadBehavior.IfNeeded"/>.</param>
    public void Download(
        ICakeContext context,
        string version,
        PlatformFamily family,
        Architecture architecture,
        DownloadBehavior behavior = DownloadBehavior.IfNeeded)
    {
        var cdxAsset = _resolver.ResolveAsset(version, family, architecture);
        var releaseAsset = new GitHubReleaseAsset(Owner, Repository, version, cdxAsset.Filename, cdxAsset.Sha256);

        context.Information("Downloading CycloneDX CLI {0} for {1}/{2}", version, family, architecture);
        _downloader.Download(context, releaseAsset, behavior);
    }
}
