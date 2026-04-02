using Cake.Common.Diagnostics;
using Cake.Common.IO;
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
        var paths = context.GetFiles(context.Environment.ApplicationRoot.CombineWithFilePath("sbom/Merged.cdx").FullPath);

        var settings = new CdxCliValidateSettings
        {
            FailOnErrors = true,
            InputFormat = CdxCliValidateInputFormat.Xml,
            InputVersion = CdxCliSpecificationVersion.V1_7
        };

        context.CdxCliValidate(paths, settings);
    }
}