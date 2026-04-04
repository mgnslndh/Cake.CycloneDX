using Cake.Common;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Release")]
[IsDependentOn(typeof(PublishTask))]
public sealed class ReleaseTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var tag = context.GitHubRefName
            ?? throw new CakeException("GITHUB_REF_NAME environment variable is not set.");

        var isPrerelease = tag.Contains('-');
        var args = isPrerelease
            ? $"release create {tag} ./artifacts/*.nupkg --generate-notes --prerelease --latest=false"
            : $"release create {tag} ./artifacts/*.nupkg --generate-notes --verify-tag --fail-on-no-commits";

        context.StartProcess("gh", new Cake.Core.IO.ProcessSettings
        {
            Arguments = args
        });
    }
}
