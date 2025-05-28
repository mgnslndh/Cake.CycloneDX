using Cake.Common.Diagnostics;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli.Validate;
using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build.Tasks;

[TaskName("Validate-Sbom")]
public sealed class ValidateSbomTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        FilePath path = context.Environment.ApplicationRoot.CombineWithFilePath("bom.xml");

        var settings = new CdxCliValidateSettings
        {
            FailOnErrors = true
        };

        context.CdxCliValidate(path, settings);
    }
}