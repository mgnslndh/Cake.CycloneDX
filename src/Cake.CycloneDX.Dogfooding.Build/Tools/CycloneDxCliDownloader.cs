using Cake.Core;
using Cake.CycloneDX.Tools.CdxCli;

namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Ensures the CycloneDX CLI tool is available, downloading and verifying
/// the correct release asset from GitHub when it is not already present.
/// </summary>
internal sealed class CycloneDxCliDownloader
{
    private const string Owner = "CycloneDX";
    private const string Repository = "cyclonedx-cli";

    private readonly GitHubReleaseDownloader _downloader = new();

    /// <summary>
    /// Downloads the CycloneDX CLI executable for <paramref name="version"/>, controlled
    /// by <paramref name="behavior"/>.
    /// </summary>
    /// <param name="context">The Cake context.</param>
    /// <param name="version">The release tag, e.g. "v0.30.0".</param>
    /// <param name="behavior">Controls when the file is downloaded. Defaults to <see cref="DownloadBehavior.IfNeeded"/>.</param>
    /// <param name="sha256">The expected lowercase hex SHA-256 hash, or <see langword="null"/> to skip verification.</param>
    public void Download(
        ICakeContext context,
        string version,
        DownloadBehavior behavior = DownloadBehavior.IfNeeded,
        string? sha256 = null)
    {
        var filename = CdxCliExecutable.GetFilename(
            context.Environment.Platform.Family,
            context.Environment.Platform.Is64Bit);
        var asset = new GitHubReleaseAsset(Owner, Repository, version, filename, sha256);
        _downloader.Download(context, asset, behavior);
    }
}
