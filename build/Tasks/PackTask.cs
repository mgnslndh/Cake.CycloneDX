using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Packs Cake.CycloneDX into ./artifacts and verifies the content of the package for this version.
/// </summary>
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
            NoRestore = true,
            OutputDirectory = "./artifacts",
        });

        var package = context.ArtifactsDirectory.CombineWithFilePath($"Cake.CycloneDX.{ThisAssembly.PackageVersion}.nupkg");
        if (!context.FileExists(package))
        {
            throw new CakeException($"Pack did not produce {package.GetFilename()}.");
        }

        var problems = PackageVerifier.Verify(package.FullPath);
        if (problems.Count > 0)
        {
            throw new CakeException($"Package {package.GetFilename()} is invalid: {string.Join("; ", problems)}");
        }

        context.Information("Verified {0}", package.GetFilename());
    }
}
