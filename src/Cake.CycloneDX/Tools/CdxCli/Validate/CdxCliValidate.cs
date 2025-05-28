using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxCli.Validate
{
    public class CdxCliValidate : CdxCliTool<CdxCliValidateSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdxCliValidate"/> class with the specified dependencies.
        /// </summary>
        public CdxCliValidate(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools) : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Lists available packages with their versions.
        /// </summary>
        /// <param name="inputFilePath">The input BOM filename.</param>
        /// <summary>
        /// Validates a CycloneDX BOM file using the cdxcli tool with the specified settings.
        /// </summary>
        /// <param name="inputFilePath">The path to the CycloneDX BOM file to validate.</param>
        /// <param name="settings">Validation options to control the behavior of the cdxcli tool.</param>
        public void Validate(FilePath inputFilePath, CdxCliValidateSettings settings)
        {
            ArgumentNullException.ThrowIfNull(inputFilePath);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentException.ThrowIfNullOrEmpty(inputFilePath.FullPath, nameof(inputFilePath));

            Run(settings, GetArguments(inputFilePath, settings));
        }

        /// <summary>
        /// Constructs the command-line arguments for the cdxcli validate command based on the provided input file path and validation settings.
        /// </summary>
        /// <param name="inputFilePath">The path to the CycloneDX BOM file to validate.</param>
        /// <param name="settings">Validation settings specifying input format, version, and error handling options.</param>
        /// <returns>A <see cref="ProcessArgumentBuilder"/> containing the arguments for the cdxcli validate command.</returns>
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
}
