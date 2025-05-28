using Cake.Common.Diagnostics;
using Cake.Core.Diagnostics;
using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build.Tasks;

[TaskName("Generate-Sbom")]
public sealed class GenerateSbomTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        CdxDotNetSettings settings = new CdxDotNetSettings
        {
            ComponentName = "Cake.CycloneDX",
            ComponentVersion = "0.0.1",
            ComponentType = CdxComponentClassification.Library,
            Output = context.Environment.ApplicationRoot,
        };
        context.CdxDotNet("src/Cake.CycloneDX.sln", settings);
    }
}