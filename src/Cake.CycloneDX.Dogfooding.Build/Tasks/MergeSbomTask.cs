using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.CycloneDX.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli.Merge;
using Cake.CycloneDX.Tools.CdxCli.Validate;
using Cake.CycloneDX.Tools.CdxDeduplicate;
using Cake.CycloneDX.Tools.CdxDotNet;
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

        string[] projects = ["Cake.CycloneDX", "Cake.CycloneDX.Tests"];

        var outputFile = context.Environment.ApplicationRoot.CombineWithFilePath("sbom/Merged.cdx");

        var inputFiles = context.GetFiles(context.Environment.ApplicationRoot.CombineWithFilePath($"*.csproj.cdx").FullPath);

        foreach (var project in projects)
        {
            settings.Name = project;
            context.CdxCliMerge(inputFiles, outputFile, settings);
        }

        context.CdxDeduplicate(outputFile, outputFile);

        var refineSettings = new CdxRefineSettings()
            .WithGroupByPurl("Cake", "^pkg:nuget/Cake")
            .WithGroupByName("Microsoft", "^Microsoft");

        context.CdxRefine(outputFile, outputFile, refineSettings);
    }
}