#nullable enable
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build;

public class BuildContext : FrostingContext
{
    public string? NuGetApiKey => Environment.GetEnvironmentVariable("NUGET_API_KEY");
    public string? GitHubRefName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

    public BuildContext(ICakeContext context)
        : base(context)
    {
        ArtifactsDirectory = context.Environment.WorkingDirectory.Combine("artifacts");
    }

    /// <summary>Gets the directory packages are written to.</summary>
    public DirectoryPath ArtifactsDirectory { get; }

    /// <summary>
    /// Gets the tag being released, from GITHUB_REF_NAME.
    /// </summary>
    public string ReleaseTag => GitHubRefName
        ?? throw new CakeException("GITHUB_REF_NAME environment variable is not set.");

    /// <summary>
    /// Gets the package matching the pushed tag: GITHUB_REF_NAME <c>v1.2.3</c> requires
    /// <c>artifacts/Cake.CycloneDX.1.2.3.nupkg</c>. Guarantees the published version equals the tag.
    /// </summary>
    public FilePath ResolveReleasePackage()
    {
        var expected = ArtifactsDirectory.CombineWithFilePath(GitHubRelease.GetPackageFileName(GitHubRefName));
        if (!this.FileExists(expected))
        {
            var found = string.Join(", ", this.GetFiles(ArtifactsDirectory.FullPath + "/*.nupkg").Select(file => file.GetFilename().FullPath));
            throw new CakeException(
                $"Tag {GitHubRefName} requires {expected.GetFilename()}, but artifacts contains: {(found.Length == 0 ? "(nothing)" : found)}.");
        }

        this.Information("Release package: {0}", expected.GetFilename());
        return expected;
    }

    /// <summary>
    /// Gets whether the GitHub Release for the tag is missing, a draft or published.
    /// </summary>
    public GitHubReleaseState GetGitHubReleaseState(string tag)
    {
        var exitCode = this.StartProcess(
            "gh",
            new ProcessSettings { Arguments = GitHubRelease.View(tag), RedirectStandardOutput = true, RedirectStandardError = true },
            out var output,
            out var error);
        return GitHubRelease.ParseViewResult(exitCode, output, error);
    }

    /// <summary>
    /// Runs the GitHub CLI and throws on a non-zero exit code.
    /// </summary>
    public void RunGitHubCli(ProcessArgumentBuilder arguments)
    {
        // StartProcess does not go through a shell, so paths are passed explicitly (no globs).
        var exitCode = this.StartProcess("gh", new ProcessSettings { Arguments = arguments });
        if (exitCode != 0)
        {
            throw new CakeException($"'gh {arguments.RenderSafe()}' failed (exit code {exitCode}).");
        }
    }
}
