using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.DotNet.Run;
using Cake.Frosting;
using Cake.Core;
using Cake;
using Cake.Core.IO;
using NuGet.Packaging;

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
            ArgumentCustomization = pab => pab
                .Append("--")
                .AppendSwitch("--verbosity", "diagnostic")
        });
    }
}