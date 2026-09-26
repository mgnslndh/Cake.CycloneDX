using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Releases the pushed tag: draft GitHub Release (Draft-Release), NuGet push (Publish), then publishes the draft.
/// NuGet is the only irreversible step; every step can be re-run if a later one fails. A tag containing '-'
/// (e.g. v1.2.0-preview.1) becomes a prerelease that is not marked latest.
/// </summary>
[TaskName("Release")]
[IsDependentOn(typeof(PublishTask))]
public sealed class ReleaseTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var tag = context.ReleaseTag;

        switch (context.GetGitHubReleaseState(tag))
        {
            case GitHubReleaseState.Draft:
                context.Information("Publishing GitHub Release {0}", tag);
                context.RunGitHubCli(GitHubRelease.Publish(tag));
                break;
            case GitHubReleaseState.Published:
                context.Information("GitHub Release {0} is already published", tag);
                break;
            case GitHubReleaseState.Missing:
                throw new CakeException($"The draft GitHub Release {tag} no longer exists; re-run the release to recreate it.");
        }
    }
}
