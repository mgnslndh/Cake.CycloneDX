using Cake.Core;
using Cake.CycloneDX.Dogfooding.Build.Tools;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build
{
    internal class BuildLifetime : FrostingLifetime<BuildContext>
    {
        private const string ToolVersion = "v0.30.0";
        private const string ToolSha256 = "1f563ba9644d2f2966fc8029fd701ca4af4f388d44c017c1d60559a1ecc9114f";

        public override void Setup(BuildContext context, ISetupContext info)
        {
            new CycloneDxCliDownloader().Download(
                context,
                ToolVersion,
                DownloadBehavior.IfNeeded,
                sha256: ToolSha256);
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {
            // do nothing
        }
    }
}
