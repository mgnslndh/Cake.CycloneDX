using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Writes the GitHub Release notes for the pushed tag to artifacts/release-notes.md, from the matching
/// CHANGELOG.md section. Fails when the section is missing or empty, so a release without notes stops before
/// anything is published. Requires GITHUB_REF_NAME.
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
