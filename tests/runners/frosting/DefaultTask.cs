using Cake.Common.IO;
using Cake.CycloneDX.Tools.CdxCli;
using Cake.CycloneDX.Tools.CdxCli.Merge;
using Cake.CycloneDX.Tools.CdxCli.Validate;
using Cake.CycloneDX.Tools.CdxDeduplicate;
using Cake.CycloneDX.Tools.CdxDotNet;
using Cake.CycloneDX.Tools.CdxRefine;
using Cake.Frosting;

namespace Frosting;

[TaskName("Default")]
public sealed class DefaultTask : FrostingTask<ScenarioContext>
{
    public override void Run(ScenarioContext context)
    {
        context.CleanDirectory(context.Output);

        foreach (var name in new[] { "App", "Lib" })
        {
            context.CdxDotNet(
                context.Scenario.CombineWithFilePath($"{name}/{name}.csproj"),
                new CdxDotNetSettings
                {
                    Output = context.Output,
                    FileName = $"{name}.cdx.xml",
                    OutputFormat = CdxDotNetOutputFormat.Xml,
                    ComponentName = name,
                    ComponentVersion = "1.0.0",
                    ComponentType = CdxComponentClassification.Library,
                });
        }

        var merged = context.Output.CombineWithFilePath("merged.cdx.xml");
        context.CdxCliMerge(
            context.GetFiles(context.Output.FullPath + "/*.cdx.xml"),
            merged,
            new CdxCliMergeSettings
            {
                Group = "com.example",
                Name = "Scenario",
                Version = "1.2.3",
                InputFormat = CdxCliMergeFormat.Xml,
                OutputFormat = CdxCliMergeFormat.Xml,
            });

        var deduplicated = context.Output.CombineWithFilePath("deduplicated.cdx.xml");
        context.CdxDeduplicate(merged, deduplicated);

        var refined = context.Output.CombineWithFilePath("refined.cdx.xml");
        context.CdxRefine(
            deduplicated,
            refined,
            new CdxRefineSettings()
                .WithAdoptOrphanedComponents()
                .WithExcludeByName("^Humanizer")
                .WithRemoveOrphanedComponents()
                .WithGroupByPurl("JamesNK", @"^pkg:nuget/Newtonsoft\.Json@")
                .WithTypeByName("framework", "^Lib$"));

        context.CdxCliValidate(
            refined,
            new CdxCliValidateSettings
            {
                InputFormat = CdxCliValidateInputFormat.Xml,
                FailOnErrors = true,
            });
    }
}
