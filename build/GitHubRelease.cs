using Cake.Core;
using Cake.Core.IO;

namespace Build;

/// <summary>
/// The state of the GitHub Release for a tag.
/// </summary>
public enum GitHubReleaseState
{
    /// <summary>No Release exists for the tag.</summary>
    Missing,

    /// <summary>A draft Release exists; it is not visible to users yet.</summary>
    Draft,

    /// <summary>The Release is published.</summary>
    Published,
}

/// <summary>
/// Builds the <c>gh release</c> commands for the draft-first release flow: create a draft Release with the package,
/// push to NuGet, then publish the draft. Every step can be re-run after a failure.
/// </summary>
public static class GitHubRelease
{
    /// <summary>Gets a value indicating whether the tag is a prerelease, e.g. <c>v1.2.0-preview.1</c>.</summary>
    public static bool IsPrerelease(string tag) => tag.Contains('-');

    /// <summary><c>gh release view</c>, printing only whether the Release is a draft.</summary>
    public static ProcessArgumentBuilder View(string tag) =>
        new ProcessArgumentBuilder()
            .Append("release").Append("view").Append(tag)
            .Append("--json").Append("isDraft")
            .Append("--jq").Append(".isDraft");

    /// <summary><c>gh release create --draft</c> with the notes from a file and the package attached.</summary>
    public static ProcessArgumentBuilder CreateDraft(string tag, FilePath package, FilePath notesFile)
    {
        var arguments = new ProcessArgumentBuilder()
            .Append("release").Append("create").Append(tag)
            .AppendQuoted(package.FullPath)
            .Append("--draft")
            .Append("--notes-file").AppendQuoted(notesFile.FullPath)
            .Append("--verify-tag");

        return IsPrerelease(tag) ? arguments.Append("--prerelease") : arguments.Append("--fail-on-no-commits");
    }

    /// <summary><c>gh release upload --clobber</c>, attaching the package to an existing (draft) Release.</summary>
    public static ProcessArgumentBuilder UploadPackage(string tag, FilePath package) =>
        new ProcessArgumentBuilder()
            .Append("release").Append("upload").Append(tag)
            .AppendQuoted(package.FullPath)
            .Append("--clobber");

    /// <summary><c>gh release edit --draft=false</c>; a stable release becomes latest, a prerelease does not.</summary>
    public static ProcessArgumentBuilder Publish(string tag)
    {
        var arguments = new ProcessArgumentBuilder()
            .Append("release").Append("edit").Append(tag)
            .Append("--draft=false");

        return IsPrerelease(tag)
            ? arguments.Append("--prerelease").Append("--latest=false")
            : arguments.Append("--latest");
    }

    /// <summary>
    /// Interprets the result of <see cref="View"/>. Only "release not found" means <see cref="GitHubReleaseState.Missing"/>;
    /// any other failure (authentication, network) throws, so it is never mistaken for a missing Release.
    /// </summary>
    public static GitHubReleaseState ParseViewResult(int exitCode, IEnumerable<string>? output, IEnumerable<string>? error)
    {
        var errorText = string.Join("\n", error ?? []).Trim();
        if (exitCode != 0)
        {
            if (errorText.Contains("release not found", StringComparison.OrdinalIgnoreCase))
            {
                return GitHubReleaseState.Missing;
            }

            throw new CakeException($"gh release view failed (exit code {exitCode}): {errorText}");
        }

        var outputText = string.Join("\n", output ?? []).Trim();
        return outputText switch
        {
            "true" => GitHubReleaseState.Draft,
            "false" => GitHubReleaseState.Published,
            _ => throw new CakeException($"Unexpected output from gh release view: '{outputText}'."),
        };
    }

    /// <summary>
    /// Gets the package file name that a release tag requires: <c>v1.2.3</c> requires <c>Cake.CycloneDX.1.2.3.nupkg</c>.
    /// </summary>
    public static string GetPackageFileName(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith('v'))
        {
            throw new CakeException($"GITHUB_REF_NAME must be a version tag like v1.2.3 (was '{tag}').");
        }

        return $"Cake.CycloneDX.{tag[1..]}.nupkg";
    }
}
