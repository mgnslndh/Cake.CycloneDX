using Cake.Common;
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Runs <c>tests/runners/script/build.cake</c> with the Cake .NET Tool at the requested Cake version.
/// </summary>
internal sealed class ScriptRunner : IRunner
{
    public string Name => "script";

    public int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory)
    {
        var toolDirectory = workDirectory.Combine("cake-tool");
        var install = context.StartProcess("dotnet", new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .Append("tool")
                .Append("install")
                .Append("Cake.Tool")
                .AppendSwitch("--version", test.CakeVersion)
                .AppendSwitchQuoted("--tool-path", toolDirectory.FullPath),
        });
        if (install != 0)
        {
            return install;
        }

        var sourceDirectory = workDirectory.Combine("src");
        NuGetConfig.Write(sourceDirectory, test.ArtifactsDirectory);

        var script = sourceDirectory.CombineWithFilePath("build.cake");
        ScriptTemplate.Render(
            test.RunnersDirectory.CombineWithFilePath("script/build.cake"),
            script,
            new Dictionary<string, string>
            {
                [@"^#addin nuget:\?package=Cake\.CycloneDX&version=[^\r\n]*"] =
                    $"#addin nuget:?package=Cake.CycloneDX&version={test.AddinVersion}",
            });

        var executable = toolDirectory.CombineWithFilePath(context.IsRunningOnWindows() ? "dotnet-cake.exe" : "dotnet-cake");
        return RunnerProcess.Run(
            context,
            test,
            executable,
            new ProcessArgumentBuilder()
                .AppendQuoted(script.FullPath)
                .AppendSwitchQuoted("--scenario", "=", test.ScenarioDirectory.FullPath)
                .AppendSwitchQuoted("--output", "=", outputDirectory.FullPath),
            sourceDirectory);
    }
}
