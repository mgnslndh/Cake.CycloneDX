using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .UseLifetime<BuildLifetime>()
                .Run(args);
        }
    }
}
