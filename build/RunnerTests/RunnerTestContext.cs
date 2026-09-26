using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Everything a runner needs to know about the current runner-test run.
/// </summary>
internal sealed record RunnerTestContext(
    string CakeVersion,
    string AddinVersion,
    DirectoryPath RepositoryRoot,
    DirectoryPath RunDirectory,
    DirectoryPath NuGetPackagesDirectory,
    DirectoryPath CycloneDxCliDirectory)
{
    public DirectoryPath RunnersDirectory => RepositoryRoot.Combine("tests/runners");

    public DirectoryPath ScenarioDirectory => RunnersDirectory.Combine("scenario");

    public DirectoryPath ArtifactsDirectory => RepositoryRoot.Combine("artifacts");
}
