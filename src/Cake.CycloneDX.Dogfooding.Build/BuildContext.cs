using Cake.Core;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build
{
    public class BuildContext : FrostingContext
    {
        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
