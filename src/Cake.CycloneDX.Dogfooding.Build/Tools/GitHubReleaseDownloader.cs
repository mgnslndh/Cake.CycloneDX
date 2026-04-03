using System.Security.Cryptography;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Net;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.CycloneDX.Dogfooding.Build.Tools;

/// <summary>
/// Downloads and verifies a single asset from a GitHub release.
/// </summary>
internal sealed class GitHubReleaseDownloader
{
    private const string GitHubDownloadBaseUrl = "https://github.com";

    /// <summary>
    /// Downloads <paramref name="asset"/> to the Cake tools directory, controlled by
    /// <paramref name="behavior"/>.
    /// </summary>
    public void Download(
        ICakeContext context,
        GitHubReleaseAsset asset,
        DownloadBehavior behavior = DownloadBehavior.IfNeeded)
    {
        var relativeToolsPath = new DirectoryPath(context.Configuration.GetValue("Paths_Tools"));
        var toolsPath = relativeToolsPath.MakeAbsolute(context.Environment);

        if (!context.DirectoryExists(toolsPath))
        {
            context.CreateDirectory(toolsPath);
        }

        var targetPath = toolsPath.CombineWithFilePath(asset.Filename);
        var toolPath = context.Tools.Resolve(asset.Filename);
        var fileExists = toolPath != null && context.FileExists(toolPath);

        if (fileExists && behavior == DownloadBehavior.IfNeeded)
        {
            context.Log.Verbose(Verbosity.Diagnostic, $"Found existing file at '{toolPath!.FullPath}'.");

            if (asset.Sha256 == null)
            {
                return;
            }

            var existingHash = ComputeHash(toolPath!.FullPath);
            if (string.Equals(existingHash, asset.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                context.Log.Verbose(Verbosity.Diagnostic, $"Existing file at '{toolPath!.FullPath}' matches the expected hash.");
                return;
            }

            // Hash mismatch — delete the stale file and re-download.
            context.Log.Verbose(Verbosity.Diagnostic, $"Existing file at '{toolPath!.FullPath}' does not match the expected hash.");
            context.DeleteFile(toolPath!.FullPath);
        }

        var downloadUrl = BuildDownloadUrl(asset);
        DownloadFile(context, downloadUrl, targetPath);

        if (asset.Sha256 != null)
        {
            VerifyChecksum(targetPath.FullPath, asset.Sha256);
        }

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(targetPath.FullPath,
                File.GetUnixFileMode(targetPath.FullPath) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }
    }

    private static Uri BuildDownloadUrl(GitHubReleaseAsset asset) =>
        new($"{GitHubDownloadBaseUrl}/{asset.Owner}/{asset.Repository}/releases/download/{asset.Tag}/{asset.Filename}");

    private static void DownloadFile(ICakeContext context, Uri downloadUrl, FilePath targetPath)
    {
        context.Verbose($"Downloading '{targetPath.GetFilename()}' from {downloadUrl}");
        try
        {
            context.DownloadFile(downloadUrl, targetPath, new DownloadFileSettings());
        }
        catch (Exception ex)
        {
            throw new CakeException(
                $"Failed to download '{targetPath.GetFilename()}' from {downloadUrl}: {ex.Message}", ex);
        }
    }

    private static string ComputeHash(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            throw new CakeException(
                $"Failed to compute SHA256 checksum for '{filePath}': {ex.Message}", ex);
        }
    }

    private static void VerifyChecksum(string filePath, string expectedSha256)
    {
        var actualHash = ComputeHash(filePath);

        if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(filePath);
            throw new CakeException(
                $"Checksum verification failed for '{System.IO.Path.GetFileName(filePath)}'.{Environment.NewLine}" +
                $"  Expected : {expectedSha256}{Environment.NewLine}" +
                $"  Actual   : {actualHash}{Environment.NewLine}" +
                "The downloaded file has been deleted.");
        }
    }
}
