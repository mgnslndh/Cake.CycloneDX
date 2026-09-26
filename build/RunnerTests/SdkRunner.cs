using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Runs <c>tests/runners/sdk/cake.cs</c> as a Cake.Sdk file-based app at the requested Cake version.
/// </summary>
internal sealed class SdkRunner : IRunner
{
    public string Name => "sdk";

    public int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory)
    {
        var sourceDirectory = workDirectory.Combine("src");
        NuGetConfig.Write(sourceDirectory, test.ArtifactsDirectory);

        var file = sourceDirectory.CombineWithFilePath("cake.cs");
        ScriptTemplate.Render(
            test.RunnersDirectory.CombineWithFilePath("sdk/cake.cs"),
            file,
            new Dictionary<string, string>
            {
                [@"^#:sdk Cake\.Sdk@[^\r\n]*"] = $"#:sdk Cake.Sdk@{test.CakeVersion}",
                [@"^#:package Cake\.CycloneDX@[^\r\n]*"] = $"#:package Cake.CycloneDX@{test.AddinVersion}",
            });

        return RunnerProcess.Run(
            context,
            test,
            "dotnet",
            new ProcessArgumentBuilder()
                .Append("run")
                .Append("--no-cache")
                .AppendSwitchQuoted("--file", file.FullPath)
                .Append("--")
                .AppendSwitchQuoted("--scenario", "=", test.ScenarioDirectory.FullPath)
                .AppendSwitchQuoted("--output", "=", outputDirectory.FullPath),
            sourceDirectory);
    }
}
