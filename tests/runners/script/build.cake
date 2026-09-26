// Cake .NET Tool runner test. RunnerTestsTask renders a copy with the real addin version.
#addin nuget:?package=Cake.CycloneDX&version=0.0.0

// --- pipeline ---
var scenario = MakeAbsolute(Directory(Argument<string>("scenario")));
var output = MakeAbsolute(Directory(Argument<string>("output")));

Task("Default").Does(() =>
{
    CleanDirectory(output);

    foreach (var name in new[] { "App", "Lib" })
    {
        CdxDotNet(
            scenario.CombineWithFilePath($"{name}/{name}.csproj"),
            new CdxDotNetSettings
            {
                Output = output,
                FileName = $"{name}.cdx.xml",
                OutputFormat = CdxDotNetOutputFormat.Xml,
                ComponentName = name,
                ComponentVersion = "1.0.0",
                ComponentType = CdxComponentClassification.Library,
            });
    }

    var merged = output.CombineWithFilePath("merged.cdx.xml");
    CdxCliMerge(
        GetFiles(output.FullPath + "/*.cdx.xml"),
        merged,
        new CdxCliMergeSettings
        {
            Group = "com.example",
            Name = "Scenario",
            Version = "1.2.3",
            InputFormat = CdxCliMergeFormat.Xml,
            OutputFormat = CdxCliMergeFormat.Xml,
        });

    var deduplicated = output.CombineWithFilePath("deduplicated.cdx.xml");
    CdxDeduplicate(merged, deduplicated);

    var refined = output.CombineWithFilePath("refined.cdx.xml");
    CdxRefine(
        deduplicated,
        refined,
        new CdxRefineSettings()
            .WithAdoptOrphanedComponents()
            .WithExcludeByName("^Humanizer")
            .WithRemoveOrphanedComponents()
            .WithGroupByPurl("JamesNK", @"^pkg:nuget/Newtonsoft\.Json@")
            .WithTypeByName("framework", "^Lib$"));

    CdxCliValidate(
        refined,
        new CdxCliValidateSettings
        {
            InputFormat = CdxCliValidateInputFormat.Xml,
            FailOnErrors = true,
        });
});

RunTarget("Default");
