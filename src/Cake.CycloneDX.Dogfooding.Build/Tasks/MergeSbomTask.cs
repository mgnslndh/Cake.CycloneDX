using Cake.Common.IO;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli.Merge;
using Cake.CycloneDX.Tools.CdxDeduplicate;
using Cake.CycloneDX.Tools.CdxRefine;
using Cake.Frosting;

namespace Cake.CycloneDX.Dogfooding.Build.Tasks;

[TaskName("Merge-Sbom")]
public sealed class MergeSbomTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new CdxCliMergeSettings
        {
            Version = "1.0",
            InputFormat = CdxCliMergeFormat.Xml,
            OutputFormat = CdxCliMergeFormat.Xml,
            OutputVersion = CdxCliSpecificationVersion.V1_6,
        };

        var outputFile = context.Environment.ApplicationRoot.CombineWithFilePath("sbom/Merged.cdx");

        var inputFiles = context.GetFiles(context.Environment.ApplicationRoot.CombineWithFilePath($"sbom/*.csproj.cdx").FullPath);

        settings.Name = "Cake.CycloneDX";
        context.CdxCliMerge(inputFiles, outputFile, settings);

        context.CdxDeduplicate(outputFile, outputFile);

        var refineSettings = new CdxRefineSettings()
            .WithGroupByPurl("Cake", "^pkg:nuget/Cake")
            .WithGroupByName("Microsoft", "^Microsoft")
            .WithGroupByName("mgnslndh", @"^Cake\.CycloneDX(\..+)?$")
            .WithTypeByBomRef("device", "^pkg:nuget/Cake.Core@");

        context.CdxRefine(outputFile, outputFile, refineSettings);
    }
}