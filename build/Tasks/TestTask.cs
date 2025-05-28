using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Test;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetTest("./src/Cake.CycloneDX.sln", new DotNetTestSettings
        {
            Configuration = "Release",
            Verbosity = DotNetVerbosity.Minimal,
            NoBuild = true
        });
    }
}
