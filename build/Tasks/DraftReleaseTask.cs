using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Creates the GitHub Release for the pushed tag as a draft (CHANGELOG notes, package attached) before anything irreversible
/// happens. A draft is invisible to users. Safe to re-run: an existing draft gets its package replaced, and an
/// already published Release is left alone.
/// </summary>
[TaskName("Draft-Release")]
[IsDependentOn(typeof(ReleaseNotesTask))]
public sealed class DraftReleaseTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var package = context.ResolveReleasePackage();
        var tag = context.ReleaseTag;

        switch (context.GetGitHubReleaseState(tag))
        {
            case GitHubReleaseState.Missing:
                context.Information("Creating draft GitHub Release {0}", tag);
                context.RunGitHubCli(GitHubRelease.CreateDraft(tag, package, context.ReleaseNotesFile));
                break;
            case GitHubReleaseState.Draft:
                context.Information("Reusing draft GitHub Release {0}; replacing its package", tag);
                context.RunGitHubCli(GitHubRelease.UploadPackage(tag, package));
                break;
            case GitHubReleaseState.Published:
                context.Information("GitHub Release {0} is already published", tag);
                break;
        }
    }
}
