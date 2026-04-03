using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Publish")]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var apiKey = context.NuGetApiKey
            ?? throw new CakeException("NUGET_API_KEY environment variable is not set.");

        var settings = new DotNetNuGetPushSettings
        {
            ApiKey = apiKey,
            Source = "https://api.nuget.org/v3/index.json",
            SkipDuplicate = true
        };

        foreach (var package in context.GetFiles("./artifacts/*.nupkg"))
            context.DotNetNuGetPush(package.FullPath, settings);

        foreach (var symbols in context.GetFiles("./artifacts/*.snupkg"))
            context.DotNetNuGetPush(symbols.FullPath, settings);
    }
}
