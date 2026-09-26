using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Builds <c>tests/runners/frosting</c> against the requested Cake.Frosting and addin versions, then runs it.
/// </summary>
internal sealed class FrostingRunner : IRunner
{
    public string Name => "frosting";

    public int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory)
    {
        var nugetConfig = NuGetConfig.Write(workDirectory, test.ArtifactsDirectory);
        var binDirectory = workDirectory.Combine("bin");
        var project = test.RunnersDirectory.CombineWithFilePath("frosting/Frosting.csproj");

        var build = RunnerProcess.Run(
            context,
            test,
            "dotnet",
            new ProcessArgumentBuilder()
                .Append("build")
                .AppendQuoted(project.FullPath)
                .Append("--force")
                .AppendSwitch("--configuration", "Release")
                .AppendSwitchQuoted("--output", binDirectory.FullPath)
                .Append($"--property:CakeVersion={test.CakeVersion}")
                .Append($"--property:AddinVersion={test.AddinVersion}")
                .AppendQuoted($"--property:RestoreConfigFile={nugetConfig.FullPath}"),
            workDirectory);
        if (build != 0)
        {
            return build;
        }

        return RunnerProcess.Run(
            context,
            test,
            "dotnet",
            new ProcessArgumentBuilder()
                .AppendQuoted(binDirectory.CombineWithFilePath("Frosting.dll").FullPath)
                .AppendSwitchQuoted("--scenario", "=", test.ScenarioDirectory.FullPath)
                .AppendSwitchQuoted("--output", "=", outputDirectory.FullPath),
            workDirectory);
    }
}
