using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Pack")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetPack("./src/Cake.CycloneDX.sln", new DotNetPackSettings
        {
            Configuration = "Release",
            Verbosity = DotNetVerbosity.Minimal,
            NoBuild = true,
            IncludeSymbols = true,
            NoRestore = true,
        });
    }
}