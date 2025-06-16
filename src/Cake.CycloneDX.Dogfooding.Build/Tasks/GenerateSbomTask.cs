using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build.Tasks;

[TaskName("Generate-Sbom")]
public sealed class GenerateSbomTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        GenerateProjectSbom(context, "Cake.CycloneDX", "0.0.1");
        GenerateProjectSbom(context, "Cake.CycloneDX.Tests", "0.0.1");
    }

    private void GenerateProjectSbom(ICakeContext context, string projectName, string projectVersion)
    {
        CdxDotNetSettings settings = new CdxDotNetSettings
        {
            ComponentName = projectName,
            ComponentVersion = projectVersion,
            Framework = "net9.0",
            ComponentType = CdxComponentClassification.Library,
            Output = context.Environment.ApplicationRoot.Combine("sbom"),
            FileName = $"{projectName}.csproj.cdx",
        };
        context.CdxDotNet($"src/{projectName}/{projectName}.csproj", settings);
    }
}