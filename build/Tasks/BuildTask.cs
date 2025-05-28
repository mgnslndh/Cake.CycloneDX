using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Build")]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetBuild("./src/Cake.CycloneDX.sln", new DotNetBuildSettings
        {
            Configuration = "Release",
            Verbosity = DotNetVerbosity.Minimal
        });
    }
}
