using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli.Validate;

namespace Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli
{
    internal class CdxCliValidateFixture : CdxCliFixture<CdxCliValidateSettings>
    {
        public FilePath InputFile { get; set; } = "bom.xml";

        /// <summary>
        /// Executes the CycloneDX CLI validation tool using the specified input file and settings.
        /// </summary>
        protected override void RunTool()
        {
            var tool = new CdxCliValidate(FileSystem, Environment, ProcessRunner, Tools);
            tool.Validate(InputFile, Settings);
        }
    }
}
