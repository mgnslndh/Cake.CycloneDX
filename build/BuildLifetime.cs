using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;

namespace Build
{
    internal class BuildLifetime : FrostingLifetime<BuildContext>
    {
        public override void Setup(BuildContext context, ISetupContext info)
        {
            context.Information("Product Version: {0}", ThisAssembly.PackageVersion);
        }

        public override void Teardown(BuildContext context, ITeardownContext info)
        {            
        }
    }
}