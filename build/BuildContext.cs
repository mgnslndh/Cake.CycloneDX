using Cake.Core;
using Cake.Frosting;

namespace Build;

public class BuildContext : FrostingContext
{
    public string? NuGetApiKey => Environment.GetEnvironmentVariable("NUGET_API_KEY");

    public BuildContext(ICakeContext context)
        : base(context)
    {
    }
}