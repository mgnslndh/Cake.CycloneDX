using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli.Merge;

namespace Cake.CycloneDX.Tests.Fixtures.Tools.CdxCli
{
    internal class CdxCliMergeFixture : CdxCliFixture<CdxCliMergeSettings>
    {
        public FilePathCollection InputFiles { get; set; } = new FilePathCollection(new FilePath[] { "a.xml", "b.xml" });

        public FilePath OutputFile { get; set; } = "merged.xml";

        protected override void RunTool()
        {
            var tool = new CdxCliMerge(FileSystem, Environment, ProcessRunner, Tools);
            tool.Merge(InputFiles, OutputFile, Settings);
        }
    }
}
