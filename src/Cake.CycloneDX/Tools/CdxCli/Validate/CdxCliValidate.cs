using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxCli.Validate;

/// <summary>
/// Runs the CycloneDX CLI <c>validate</c> command. Build scripts should use the
/// <see cref="CdxCliAliases.CdxCliValidate(Cake.Core.ICakeContext, FilePath, CdxCliValidateSettings?)"/> alias instead.
/// </summary>
public class CdxCliValidate : CdxCliTool<CdxCliValidateSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CdxCliValidate"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="processRunner">The process runner.</param>
    /// <param name="tools">The tool locator.</param>
    public CdxCliValidate(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools) : base(fileSystem, environment, processRunner, tools)
    {
    }

    /// <summary>
    /// Validates a CycloneDX BOM against the CycloneDX schema.
    /// </summary>
    /// <param name="inputFilePath">The BOM file to validate.</param>
    /// <param name="settings">The settings.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePath"/> is empty.</exception>
    /// <exception cref="CakeException">The CycloneDX CLI cannot be found or exits with a non-zero exit code.</exception>
    public void Validate(FilePath inputFilePath, CdxCliValidateSettings settings)
    {
        ArgumentNullException.ThrowIfNull(inputFilePath);
        Throw.IfFullPathIsNullOrWhitespace(inputFilePath, nameof(inputFilePath));
        ArgumentNullException.ThrowIfNull(settings);

        Run(settings, GetArguments(inputFilePath, settings));
    }

    private ProcessArgumentBuilder GetArguments(FilePath inputFilePath, CdxCliValidateSettings settings)
    {
        var builder = new ProcessArgumentBuilder();

        builder.Append("validate");

        builder.AppendSwitchQuoted("--input-file", inputFilePath.FullPath);

        if (settings.InputFormat is not null)
        {
            builder.AppendSwitch("--input-format", $"{settings.InputFormat}");
        }

        if (settings.InputVersion is not null)
        {
            builder.AppendSwitch("--input-version", $"{settings.InputVersion}");
        }

        if (settings.FailOnErrors)
        {
            builder.Append("--fail-on-errors");
        }

        return builder;
    }
}