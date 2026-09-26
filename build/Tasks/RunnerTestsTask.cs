using System.Xml.Linq;
using Build.RunnerTests;
using Build.Tools;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Runs the scenario pipeline on every Cake runner against the packed addin. Usage:
/// <c>.\build.ps1 --target RunnerTests [--cake-version 6.0.0|6.*]</c>.
/// </summary>
[TaskName("RunnerTests")]
[IsDependentOn(typeof(PackTask))]
public sealed class RunnerTestsTask : FrostingTask<BuildContext>
{
    private const string CycloneDxCliVersion = "v0.30.0";

    private static readonly IRunner[] Runners =
    [
        new ScriptRunner(),
        new SdkRunner(),
        new FrostingRunner(),
    ];

    public override void Run(BuildContext context)
    {
        EnsureIdenticalScriptPipelines(context);

        var cakeVersion = CakeVersionResolver.Resolve(context.Argument("cake-version", "6.*"));
        var addinVersion = ThisAssembly.PackageVersion;
        var root = context.Environment.WorkingDirectory;

        var package = root.CombineWithFilePath($"artifacts/Cake.CycloneDX.{addinVersion}.nupkg");
        if (!context.FileExists(package))
        {
            throw new CakeException($"Package '{package.FullPath}' was not found. Run the Pack target first.");
        }

        context.Information("Runner tests: Cake {0}, Cake.CycloneDX {1}", cakeVersion, addinVersion);

        EnsureCycloneDxDotNetTool(context);
        new CycloneDxCliDownloader(new CycloneDxReleaseManifestResolver()).Download(context, CycloneDxCliVersion);
        var cliDirectory = new DirectoryPath(context.Configuration.GetValue("Paths_Tools")).MakeAbsolute(context.Environment);

        var runDirectory = root.Combine($"artifacts/runner-tests/{cakeVersion}");
        context.EnsureDirectoryExists(runDirectory);
        context.CleanDirectory(runDirectory);

        var test = new RunnerTestContext(
            cakeVersion,
            addinVersion,
            root,
            runDirectory,
            runDirectory.Combine("nuget-packages"),
            cliDirectory);

        var results = Runners.Select(runner => RunOne(context, test, runner)).ToList();
        CompareOutputs(results);
        Report(context, results);
    }

    private static RunnerResult RunOne(ICakeContext context, RunnerTestContext test, IRunner runner)
    {
        var result = new RunnerResult(runner.Name);
        var workDirectory = test.RunDirectory.Combine(runner.Name);
        var outputDirectory = workDirectory.Combine("out");
        context.EnsureDirectoryExists(outputDirectory);

        context.Information("=== Runner: {0} ===", runner.Name);
        int exitCode;
        try
        {
            exitCode = runner.Run(context, test, workDirectory, outputDirectory);
        }
        catch (Exception exception)
        {
            result.Failures.Add($"runner threw: {exception.Message}");
            return result;
        }

        if (exitCode != 0)
        {
            result.Failures.Add($"exited with code {exitCode}");
            return result;
        }

        var sbomFile = outputDirectory.CombineWithFilePath("refined.cdx.xml");
        if (!context.FileExists(sbomFile))
        {
            result.Failures.Add($"'{sbomFile.FullPath}' was not produced");
            return result;
        }

        result.Sbom = XDocument.Load(sbomFile.FullPath);
        result.Failures.AddRange(SbomAssertions.Check(result.Sbom));
        return result;
    }

    private static void CompareOutputs(IReadOnlyList<RunnerResult> results)
    {
        var reference = results.FirstOrDefault(result => result.Sbom is not null);
        if (reference is null)
        {
            return;
        }

        foreach (var result in results.Where(result => result.Sbom is not null && result != reference))
        {
            var difference = SbomNormalizer.FindFirstDifference(reference.Sbom!, result.Sbom!);
            if (difference is not null)
            {
                result.Failures.Add($"SBOM differs from the {reference.Name} runner's at {difference}");
            }
        }
    }

    private static void Report(ICakeContext context, IReadOnlyList<RunnerResult> results)
    {
        context.Information("=== Runner test summary ===");
        foreach (var result in results)
        {
            var name = result.Name.PadRight(10);
            if (result.Passed)
            {
                context.Information("  {0} passed", name);
                continue;
            }

            context.Error("  {0} FAILED", name);
            foreach (var failure in result.Failures)
            {
                context.Error("      {0}", failure);
            }
        }

        if (results.Any(result => !result.Passed))
        {
            throw new CakeException("Runner tests failed. See the summary above.");
        }
    }

    private static void EnsureIdenticalScriptPipelines(ICakeContext context)
    {
        var runners = context.Environment.WorkingDirectory.Combine("tests/runners");
        var script = ScriptTemplate.GetPipeline(runners.CombineWithFilePath("script/build.cake"));
        var sdk = ScriptTemplate.GetPipeline(runners.CombineWithFilePath("sdk/cake.cs"));
        if (!string.Equals(script, sdk, StringComparison.Ordinal))
        {
            throw new CakeException(
                "The pipelines in tests/runners/script/build.cake and tests/runners/sdk/cake.cs differ. "
                + $"Everything after '{ScriptTemplate.PipelineMarker}' must be identical.");
        }
    }

    private static void EnsureCycloneDxDotNetTool(ICakeContext context)
    {
        int exitCode;
        try
        {
            exitCode = context.StartProcess("dotnet-CycloneDX", new ProcessSettings { Arguments = "--version" });
        }
        catch (Exception)
        {
            exitCode = -1;
        }

        if (exitCode != 0)
        {
            throw new CakeException("The CycloneDX .NET tool is not installed. Install it with: dotnet tool install -g CycloneDX");
        }
    }
}
