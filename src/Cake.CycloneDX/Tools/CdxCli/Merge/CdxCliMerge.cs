using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.CycloneDX.Tools.CdxCli.Validate;

namespace Cake.CycloneDX.Tools.CdxCli.Merge;

public class CdxCliMerge : CdxCliTool<CdxCliMergeSettings>
{
    public CdxCliMerge(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools)
        : base(fileSystem, environment, processRunner, tools)
    {
    }

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

        if (settings.Name is not null)
        {
            builder.AppendSwitchQuoted("--name", settings.Name);
        }

        if (settings.Name is not null)
        {
            builder.AppendSwitchQuoted("--version", settings.Version);
        }

        return builder;
    }
}