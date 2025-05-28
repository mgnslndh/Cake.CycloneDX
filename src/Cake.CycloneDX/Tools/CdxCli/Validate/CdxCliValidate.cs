using Cake.Core;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.CycloneDX.Tools.CdxCli.Validate
{
    public class CdxCliValidate : CdxCliTool<CdxCliValidateSettings>
    {
        public CdxCliValidate(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner, IToolLocator tools) : base(fileSystem, environment, processRunner, tools)
        {
        }

        /// <summary>
        /// Validates a CycloneDX BOM file using the CycloneDX CLI tool.
        /// </summary>
        /// <param name="inputFilePath">The input BOM filename.</param>
        /// <param name="settings">The settings.</param>
        public void Validate(FilePath inputFilePath, CdxCliValidateSettings settings)
        {
            ArgumentNullException.ThrowIfNull(inputFilePath);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentException.ThrowIfNullOrEmpty(inputFilePath.FullPath, nameof(inputFilePath));

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
}
