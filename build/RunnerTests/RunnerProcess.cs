using Cake.Common;
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Starts runner processes with the CycloneDX CLI on the PATH and an isolated NuGet package folder, so a locally
/// rebuilt addin that keeps its version is never served from a stale cache.
/// </summary>
internal static class RunnerProcess
{
    public static int Run(
        ICakeContext context,
        RunnerTestContext test,
        FilePath executable,
        ProcessArgumentBuilder arguments,
        DirectoryPath workingDirectory)
    {
        var path = test.CycloneDxCliDirectory.FullPath
            + System.IO.Path.PathSeparator
            + (Environment.GetEnvironmentVariable("PATH") ?? string.Empty);

        return context.StartProcess(executable, new ProcessSettings
        {
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["PATH"] = path,
                ["NUGET_PACKAGES"] = test.NuGetPackagesDirectory.FullPath,
            },
        });
    }
}
