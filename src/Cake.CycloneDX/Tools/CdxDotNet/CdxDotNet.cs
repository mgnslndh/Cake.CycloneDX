using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxDotNet;

/// <summary>
/// A .NET Core global tool which creates CycloneDX Software Bill-of-Materials (SBOM) from .NET projects.
/// </summary>
public class CdxDotNet : Tool<CdxDotNetSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdxDotNet"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="processRunner">The process runner.</param>
    /// <param name="tools">The tool locator.</param>
    public CdxDotNet(
        IFileSystem fileSystem,
        ICakeEnvironment environment,
        IProcessRunner processRunner,
        IToolLocator tools)
        : base(fileSystem, environment, processRunner, tools)
    {
    }

    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    /// <returns>The name of the tool.</returns>
    protected sealed override string GetToolName()
    {
        return "CycloneDX .NET Tool";
    }

    /// <summary>
    /// Gets the possible names of the tool executable.
    /// </summary>
    /// <returns>The tool executable name.</returns>
    protected override IEnumerable<string> GetToolExecutableNames()
    {
        return ["dotnet-CycloneDX.exe", "dotnet-CycloneDX"];
    }

    public void Run(string path, CdxDotNetSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        var arguments = GetArguments(path, settings);
        Run(settings, arguments);
    }

    private ProcessArgumentBuilder GetArguments(string path, CdxDotNetSettings settings)
    {
        var builder = new ProcessArgumentBuilder();

        builder.AppendQuoted(path);

        if (settings.Framework is not null)
        {
            builder.AppendSwitch("--framework", settings.Framework);
        }

        if (settings.Runtime is not null)
        {
            builder.AppendSwitch("--runtime", settings.Runtime);
        }

        if (settings.Output is not null)
        {
            builder.AppendSwitchQuoted("--output", settings.Output.FullPath);
        }

        if (settings.FileName is not null)
        {
            builder.AppendSwitch("--filename", settings.FileName);
        }

        if (settings.ExcludeDevelopmentDependencies)
        {
            builder.Append("--exclude-dev");
        }

        if (settings.ExcludeTestProjects)
        {
            builder.Append("--exclude-test-projects");
        }

        if (settings.Recursive)
        {
            builder.Append("--recursive");
        }

        if (settings.DisablePackageRestore)
        {
            builder.Append("--disable-package-restore");
        }

        if (settings.IncludeProjectReferences)
        {
            builder.Append("--include-project-references");
        }

        if (settings.ExcludeFilters.Count > 0)
        {
            builder.AppendSwitchQuoted("--exclude-filter", settings.ExcludeFilters.ToArgumentString());
        }

        if (settings.ComponentName is not null)
        {
            builder.AppendSwitchQuoted("--set-name", settings.ComponentName);
        }

        if (settings.ComponentVersion is not null)
        {
            builder.AppendSwitch("--set-version", settings.ComponentVersion);
        }

        if (settings.ComponentType is not null)
        {
            builder.AppendSwitch("--set-type", settings.ComponentType.ToString());
        }

        if (settings.SpecVersion is not null)
        {
            builder.AppendSwitch("--spec-version", settings.SpecVersion.Value.ToVersionString());
        }

        if (settings.OutputFormat is not null)
        {
            builder.AppendSwitch("--output-format", settings.OutputFormat.ToString());
        }

        return builder;
    }
}
