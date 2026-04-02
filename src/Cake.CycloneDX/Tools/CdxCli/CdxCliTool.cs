using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxCli;

/// <summary>
/// Base class for all CycloneDX CLI related tools.
/// </summary>
/// <typeparam name="TSettings">The settings type.</typeparam>
public abstract class CdxCliTool<TSettings> : Tool<TSettings>
    where TSettings : ToolSettings
{
    protected ICakeEnvironment Environment { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CdxCliTool{TSettings}"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="processRunner">The process runner.</param>
    /// <param name="tools">The tool locator.</param>
    protected CdxCliTool(
        IFileSystem fileSystem,
        ICakeEnvironment environment,
        IProcessRunner processRunner,
        IToolLocator tools)
        : base(fileSystem, environment, processRunner, tools)
    {
        Environment = environment;
    }

    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    /// <returns>The name of the tool.</returns>
    protected sealed override string GetToolName()
    {
        return "CycloneDX CLI";
    }

    /// <summary>
    /// Gets the possible names of the tool executable.
    /// </summary>
    /// <returns>The tool executable name.</returns>
    protected override IEnumerable<string> GetToolExecutableNames()
    {
        return
        [
            CdxCliExecutable.GetFilename(Environment.Platform.Family, Environment.Platform.Is64Bit),
            "cyclonedx"
        ];
    }
}