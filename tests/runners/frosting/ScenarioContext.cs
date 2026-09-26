using Cake.Common;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Frosting;

public sealed class ScenarioContext : FrostingContext
{
    public ScenarioContext(ICakeContext context)
        : base(context)
    {
        Scenario = context.MakeAbsolute(new DirectoryPath(context.Argument<string>("scenario")));
        Output = context.MakeAbsolute(new DirectoryPath(context.Argument<string>("output")));
    }

    public DirectoryPath Scenario { get; }

    public DirectoryPath Output { get; }
}
