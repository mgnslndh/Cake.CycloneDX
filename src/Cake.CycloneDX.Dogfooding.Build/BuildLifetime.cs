using Cake.Core;
using Cake.CycloneDX.Dogfooding.Build.Tools;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build
{
    internal class BuildLifetime : FrostingLifetime<BuildContext>
    {
        private const string ToolVersion = "v0.30.0";

        public override void Setup(BuildContext context, ISetupContext info)
        {
            var resolver = new CycloneDxReleaseManifestResolver();
            var downloader = new CycloneDxCliDownloader(resolver);
            downloader.Download(context, ToolVersion);
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {
            // do nothing
        }
    }
}
