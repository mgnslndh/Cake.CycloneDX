#nullable enable
using Cake.Core;
using Cake.Frosting;

namespace Build;

public class BuildContext : FrostingContext
{
    public string? NuGetApiKey => Environment.GetEnvironmentVariable("NUGET_API_KEY");
    public string? GitHubRefName => Environment.GetEnvironmentVariable("GITHUB_REF_NAME");

    public BuildContext(ICakeContext context)
        : base(context)
    {
    }
}