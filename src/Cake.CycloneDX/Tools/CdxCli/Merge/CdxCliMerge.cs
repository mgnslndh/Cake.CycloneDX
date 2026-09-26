using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.CycloneDX.Tools.CdxCli.Validate;

namespace Cake.CycloneDX.Tools.CdxCli.Merge;

/// <summary>
/// Runs the CycloneDX CLI <c>merge</c> command. Build scripts should use the
/// <see cref="CdxCliAliases.CdxCliMerge"/> alias instead.
/// </summary>
public class CdxCliMerge : CdxCliTool<CdxCliMergeSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdxCliMerge"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="processRunner">The process runner.</param>
    /// <param name="tools">The tool locator.</param>
    public CdxCliMerge(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
        : base(fileSystem, environment, processRunner, tools)
    {
    }

    /// <summary>
    /// Merges two or more CycloneDX BOMs into a single BOM.
    /// </summary>
    /// <param name="inputFilePaths">The BOM files to merge. Must contain at least one path.</param>
    /// <param name="outputFilePath">The file to write the merged BOM to.</param>
    /// <param name="settings">The settings.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePaths"/> is empty or contains an empty path, or <paramref name="outputFilePath"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">A hierarchical merge is requested without a name or a version.</exception>
    /// <exception cref="CakeException">The CycloneDX CLI cannot be found or exits with a non-zero exit code.</exception>
    public void Merge(FilePathCollection inputFilePaths, FilePath outputFilePath, CdxCliMergeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        if (inputFilePaths.Count == 0)
        {
            throw new ArgumentException("Must provide at least one input path", nameof(inputFilePaths));
        }

        ArgumentNullException.ThrowIfNull(settings);

        if (inputFilePaths.Any(path => string.IsNullOrWhiteSpace(path.FullPath)))
        {
            throw new ArgumentException("Invalid input file path", nameof(inputFilePaths));
        }

        ArgumentNullException.ThrowIfNull(outputFilePath);
        ArgumentException.ThrowIfNullOrEmpty(outputFilePath.FullPath, nameof(outputFilePath));

        Run(settings, GetArguments(inputFilePaths, outputFilePath, settings));
    }

    private ProcessArgumentBuilder GetArguments(FilePathCollection inputFilePaths, FilePath outputFilePath, CdxCliMergeSettings settings)
    {
        var builder = new ProcessArgumentBuilder();

        builder.Append("merge");

        builder.Append("--input-files");
        foreach (var inputFilePath in inputFilePaths)
        {
            builder.AppendQuoted(inputFilePath.FullPath);
        }

        if (settings.InputFormat is not null)
        {
            builder.AppendSwitch("--input-format", $"{settings.InputFormat}");
        }

        builder.AppendSwitchQuoted("--output-file", outputFilePath.FullPath);

        if (settings.OutputFormat is not null)
        {
            builder.AppendSwitch("--output-format", $"{settings.OutputFormat}");
        }

        if (settings.OutputVersion is not null)
        {
            builder.AppendSwitch("--output-version", $"{settings.OutputVersion}");
        }

        if (settings.Hierarchical)
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                throw new InvalidOperationException(
                    $"The '{nameof(settings.Name)}' setting is required when using the hierarchical setting.");
            }

            if (string.IsNullOrWhiteSpace(settings.Version))
            {
                throw new InvalidOperationException(
                    $"The '{nameof(settings.Version)}' setting is required when using the hierarchical setting.");
            }

            builder.Append("--hierarchical");
        }

        if (settings.Group is not null)
        {
            builder.AppendSwitchQuoted("--group", settings.Group);
        }

        if (settings.Name is not null)
        {
            builder.AppendSwitchQuoted("--name", settings.Name);
        }

        if (settings.Version is not null)
        {
            builder.AppendSwitchQuoted("--version", settings.Version);
        }

        return builder;
    }
}