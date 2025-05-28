using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Run;
using Cake.Frosting;

namespace Build.Tasks;

[TaskName("Dogfood")]
[IsDependentOn(typeof(BuildTask))]
public sealed class DogfoodTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRun("./src/Cake.CycloneDX.Dogfooding.Build/Cake.CycloneDX.Dogfooding.Build.csproj", new DotNetRunSettings
        {
            Configuration = "Release",
            Verbosity = DotNetVerbosity.Diagnostic,
            NoBuild = true,
            NoRestore = true,
        });
    }
}