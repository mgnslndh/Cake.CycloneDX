using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Pushes the package that matches the pushed tag to nuget.org, after the draft GitHub Release exists.
/// Requires NUGET_API_KEY and GITHUB_REF_NAME, and a package produced by a previous Pack. Safe to re-run
/// (duplicates are skipped).
/// </summary>
[TaskName("Publish")]
[IsDependentOn(typeof(DraftReleaseTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var package = context.ResolveReleasePackage();
        var apiKey = context.NuGetApiKey
            ?? throw new CakeException("NUGET_API_KEY environment variable is not set.");

        context.DotNetNuGetPush(package, new DotNetNuGetPushSettings
        {
            ApiKey = apiKey,
            Source = "https://api.nuget.org/v3/index.json",
            SkipDuplicate = true
        });
    }
}
