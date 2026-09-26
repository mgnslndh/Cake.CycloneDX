using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// The release gate for the documentation that ships with a release. Checks that CHANGELOG.md is ready for the pushed
/// tag and writes the tag's section to artifacts/release-notes.md as the GitHub Release notes. Fails when the section
/// is missing, empty or not the newest version, or when [Unreleased] still has entries a stable release would leave
/// out. For a stable tag it also fails when the README's install snippets name another version. The release stops
/// before anything is built or published. Requires GITHUB_REF_NAME. See <see cref="ChangelogReleaseNotes"/> and
/// <see cref="ReadmeInstallVersion"/>.
/// </summary>
[TaskName("Release-Notes")]
public sealed class ReleaseNotesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var tag = context.ReleaseTag;
        var root = context.Environment.WorkingDirectory;
        var changelog = File.ReadAllText(root.CombineWithFilePath("CHANGELOG.md").FullPath);
        var notes = ChangelogReleaseNotes.Extract(changelog, tag);

        ReadmeInstallVersion.Check(File.ReadAllText(root.CombineWithFilePath("README.md").FullPath), tag);

        context.EnsureDirectoryExists(context.ArtifactsDirectory);
        File.WriteAllText(context.ReleaseNotesFile.FullPath, notes + "\n");

        context.Information("Release notes for {0}:\n{1}", tag, notes);
    }
}
