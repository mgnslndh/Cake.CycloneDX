using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// A Cake runner under test. Runs the scenario pipeline and writes <c>refined.cdx.xml</c> to the output directory.
/// </summary>
internal interface IRunner
{
    string Name { get; }

    /// <returns>The exit code of the runner; 0 means success.</returns>
    int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory);
}
