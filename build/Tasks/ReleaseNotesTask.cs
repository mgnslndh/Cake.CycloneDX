using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// The release gate for CHANGELOG.md: checks that it is ready for the pushed tag and writes the tag's section to
/// artifacts/release-notes.md as the GitHub Release notes. Fails when the section is missing, empty or not the
/// newest version, or when [Unreleased] still has entries a stable release would leave out, so the release stops
/// before anything is built or published. Requires GITHUB_REF_NAME. See <see cref="ChangelogReleaseNotes"/>.
/// </summary>
[TaskName("Release-Notes")]
public sealed class ReleaseNotesTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var tag = context.ReleaseTag;
        var changelog = File.ReadAllText(context.Environment.WorkingDirectory.CombineWithFilePath("CHANGELOG.md").FullPath);
        var notes = ChangelogReleaseNotes.Extract(changelog, tag);

        context.EnsureDirectoryExists(context.ArtifactsDirectory);
        File.WriteAllText(context.ReleaseNotesFile.FullPath, notes + "\n");

        context.Information("Release notes for {0}:\n{1}", tag, notes);
    }
}
