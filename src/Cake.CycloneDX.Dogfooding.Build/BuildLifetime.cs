using Cake.Common.IO;
using Cake.Common.Net;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build
{
    internal class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context, ISetupContext info)
        {
            var relativeToolsPath = new DirectoryPath(context.Configuration.GetValue("Paths_Tools"));
            var toolsPath = relativeToolsPath.MakeAbsolute(context.Environment);

            if (!context.DirectoryExists(toolsPath))
            {
                context.CreateDirectory(toolsPath);
            }

            var filename = "cyclonedx-win-x64.exe";

            var toolPath = context.Tools.Resolve(filename);

            if (toolPath == null || !context.FileExists(toolPath))
            {
                var targetPath = toolsPath.CombineWithFilePath(filename);
                context.DownloadFile(new Uri("https://github.com/CycloneDX/cyclonedx-cli/releases/download/v0.27.2/cyclonedx-win-x64.exe"), targetPath, new DownloadFileSettings());
            }
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {
            // do nothing
        }
    }
}
