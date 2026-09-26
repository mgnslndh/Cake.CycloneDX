# Cake.CycloneDX

[![NuGet](https://img.shields.io/nuget/v/Cake.CycloneDX.svg)](https://www.nuget.org/packages/Cake.CycloneDX)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cake.CycloneDX.svg)](https://www.nuget.org/packages/Cake.CycloneDX)
[![CI](https://github.com/mgnslndh/Cake.CycloneDX/actions/workflows/main.yml/badge.svg)](https://github.com/mgnslndh/Cake.CycloneDX/actions/workflows/main.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/mgnslndh/Cake.CycloneDX/blob/main/LICENSE)
![CodeRabbit Pull Request Reviews](https://img.shields.io/coderabbit/prs/github/mgnslndh/Cake.CycloneDX?logo=red&logoColor=%23FF570A)

A [Cake](https://cakebuild.net) addin for [CycloneDX](https://cyclonedx.org) software bills of materials (SBOMs). It runs
the [CycloneDX .NET tool](https://github.com/CycloneDX/cyclonedx-dotnet) and the
[CycloneDX CLI](https://github.com/CycloneDX/cyclonedx-cli), and deduplicates and refines XML SBOMs in C#.

## Table of contents

- [Installation](#installation)
- [Tools](#tools)
- [Typical pipeline](#typical-pipeline)
- [CdxDotNet](#cdxdotnet)
- [CdxCliMerge](#cdxclimerge)
- [CdxDeduplicate](#cdxdeduplicate)
- [CdxRefine](#cdxrefine)
- [CdxCliValidate](#cdxclivalidate)
- [Changelog](#changelog)
- [License](#license)

## Installation

Cake script (.NET Tool runner):

```csharp
#addin nuget:?package=Cake.CycloneDX&version=0.0.5
```

Cake SDK (file-based `dotnet cake.cs`):

```csharp
#:sdk Cake.Sdk@6.3.0
#:package Cake.CycloneDX@0.0.5
```

Cake Frosting: add a package reference to `Cake.CycloneDX`. The aliases are extension methods on the context, e.g.
`context.CdxDotNet(...)`.

The addin targets `net8.0`, `net9.0` and `net10.0` and supports Cake 6.0.0 and later (0.0.5 needs Cake 6.1.0). Every
build tests the packed addin on all three runners with Cake 6.0.0 and the latest 6.x, on Windows, Linux and macOS.

This README describes the `main` branch. See the [changelog](#changelog) for what each version includes.

## Tools

`CdxDotNet`, `CdxCliMerge` and `CdxCliValidate` run external tools. `CdxDeduplicate` and `CdxRefine` need no tools.

| Tool | Used by | Install |
|---|---|---|
| [CycloneDX .NET tool](https://github.com/CycloneDX/cyclonedx-dotnet) 6.x | `CdxDotNet` | `dotnet tool install -g CycloneDX` |
| [CycloneDX CLI](https://github.com/CycloneDX/cyclonedx-cli) | `CdxCliMerge`, `CdxCliValidate` | Download the binary for your platform from the [releases](https://github.com/CycloneDX/cyclonedx-cli/releases) |

The addin looks for the tools in Cake's tools folder and on `PATH`:

- The CycloneDX .NET tool as `dotnet-CycloneDX`.
- The CycloneDX CLI under its release file name for the current platform (such as `cyclonedx-win-x64.exe`,
  `cyclonedx-linux-x64` or `cyclonedx-osx-arm64`), then as `cyclonedx` or `cyclonedx.exe`.

Set `ToolPath` in the settings to use a tool from anywhere else. The build tests the addin with the CycloneDX .NET tool
6.2.0 and the CycloneDX CLI 0.30.0.

## Typical pipeline

Create an SBOM per project, merge them, remove duplicates, shape the result and validate it. This is the pipeline the
build runs on every runner:

```csharp
Task("Sbom").Does(() =>
{
    var output = MakeAbsolute(Directory("./artifacts/sbom"));
    CleanDirectory(output);

    foreach (var name in new[] { "App", "Lib" })
    {
        CdxDotNet($"./src/{name}/{name}.csproj", new CdxDotNetSettings
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
    CdxCliMerge(GetFiles(output.FullPath + "/*.cdx.xml"), merged, new CdxCliMergeSettings
    {
        Group = "com.example",
        Name = "Example",
        Version = "1.2.3",
        InputFormat = CdxCliMergeFormat.Xml,
        OutputFormat = CdxCliMergeFormat.Xml,
    });

    var deduplicated = output.CombineWithFilePath("deduplicated.cdx.xml");
    CdxDeduplicate(merged, deduplicated);

    var refined = output.CombineWithFilePath("refined.cdx.xml");
    CdxRefine(deduplicated, refined, new CdxRefineSettings()
        .WithAdoptOrphanedComponents()
        .WithExcludeByName("^Humanizer")
        .WithRemoveOrphanedComponents()
        .WithGroupByPurl("JamesNK", @"^pkg:nuget/Newtonsoft\.Json@"));

    CdxCliValidate(refined, new CdxCliValidateSettings
    {
        InputFormat = CdxCliValidateInputFormat.Xml,
        FailOnErrors = true,
    });
});
```

`CdxDeduplicate` and `CdxRefine` only read and write XML, so ask the tools for XML when you use them.

## CdxDotNet

`CdxDotNet` runs the CycloneDX .NET tool on a solution (`.sln`, `.slnf`, `.slnx`), a project, a `packages.config` file,
or a directory that is searched for `packages.config` files. The path can be a `FilePath`, a `DirectoryPath` or a string.

```csharp
CdxDotNet("./src/MySolution.sln", new CdxDotNetSettings
{
    Output = "./artifacts",
    FileName = "bom.xml",
    OutputFormat = CdxDotNetOutputFormat.Xml,
    ExcludeDevelopmentDependencies = true,
    ExcludeTestProjects = true,
}.WithComponentName("MyProduct")
 .WithComponentVersion("1.2.3")
 .WithComponentType(CdxComponentClassification.Application)
 .WithExcludeFilter("Newtonsoft.Json", "12.0.1"));
```

Each `CdxDotNetSettings` property maps to a tool option, and properties left at their default aren't passed:

| Setting | Purpose |
|---|---|
| `Output`, `FileName` | Where to write the SBOM. The tool defaults to `bom.xml` or `bom.json`. |
| `OutputFormat` | `Xml`, `Json`, `UnsafeJson` or `Auto`. |
| `SpecVersion` | The CycloneDX specification version to write. |
| `Framework`, `Runtime` | Analyze one target framework or runtime instead of aggregating all of them. |
| `ComponentName`, `ComponentVersion`, `ComponentType` | The metadata component that describes the software. |
| `ExcludeDevelopmentDependencies`, `ExcludeTestProjects` | Leave out analyzers and other development dependencies, or test projects. |
| `ExcludeFilters` | Packages to leave out, with their transitive dependencies. `WithExcludeFilter(name)` excludes every version, `WithExcludeFilter(name, version)` one. |
| `Recursive`, `IncludeProjectReferences` | For a single project: also scan its project references, or list them as components. |
| `DisablePackageRestore` | Skip the restore the tool runs before analyzing. |

## CdxCliMerge

`CdxCliMerge` merges several SBOMs into one with `cyclonedx merge`.

```csharp
CdxCliMerge(
    GetFiles("./artifacts/sbom/*.cdx.xml"),
    "./artifacts/merged.cdx.xml",
    new CdxCliMergeSettings
    {
        Group = "com.example",
        Name = "MyProduct",
        Version = "1.2.3",
        InputFormat = CdxCliMergeFormat.Xml,
        OutputFormat = CdxCliMergeFormat.Xml,
    });
```

- `Group`, `Name` and `Version` describe the merged software in the metadata component.
- `InputFormat` and `OutputFormat` are `Xml`, `Json`, `Protobuf` or `AutoDetect`. The CLI detects the input format,
  and derives the output format from the file extension, when they aren't set.
- `OutputVersion` sets the CycloneDX specification version of the merged SBOM.
- `Hierarchical = true` nests each input SBOM's components under its metadata component instead of merging them into
  one flat list. It requires `Name` and `Version`, and every input SBOM needs a metadata component.

A flat merge leaves the original project components as top-level components that nothing depends on. Run
[`CdxRefine`](#cdxrefine) with `WithAdoptOrphanedComponents()` to link them back into the dependency tree.

## CdxDeduplicate

`CdxDeduplicate` removes duplicate top-level components from an XML SBOM, for example after a merge. It needs no tool.

```csharp
CdxDeduplicate("./artifacts/merged.cdx.xml", "./artifacts/deduplicated.cdx.xml");
```

- By default, components are duplicates when they share a `bom-ref` or a purl. Turn either check off with
  `DeduplicateByBomRef` or `DeduplicateByPurl` in `CdxDeduplicateSettings`.
- The first component of each group is kept, and a warning is logged for each group that had duplicates.
- Dependency references to a removed component are redirected to the kept one.
- Components without a `bom-ref` or purl are kept, after the deduplicated ones.

There are also overloads that take the SBOM as an XML string or an `XDocument`.

## CdxRefine

`CdxRefine` shapes an existing XML SBOM's component set and dependency graph, typically one
produced by `CdxDotNet` and merged with `CdxCliMerge`. It needs no tool. It runs these steps, in order, skipping
any step that has no settings:

1. **Adopt** orphaned components into the dependency tree (repair a broken tree).
2. **Exclude** components matching a name, purl or bom-ref pattern.
3. **Remove orphaned components** left unreachable after exclusion, or left dangling for other
   reasons.
4. **Group** and **Type**, applied only to what is left.

Each step has settings that match components by name, purl or bom-ref, using regular expressions:

| Step | Settings |
|---|---|
| Adopt | `WithAdoptOrphanedComponents()`, or `WithAdoptionByName`, `WithAdoptionByPurl` and `WithAdoptionByBomRef` to choose the orphans and their parent |
| Exclude | `WithExcludeByName`, `WithExcludeByPurl`, `WithExcludeByBomRef` |
| Remove orphans | `WithRemoveOrphanedComponents()` |
| Group | `WithGroupByName`, `WithGroupByPurl`, `WithGroupByBomRef` |
| Type | `WithTypeByName`, `WithTypeByPurl`, `WithTypeByBomRef` |

A `cyclonedx-cli merge` in flat mode produces a *broken tree*: the original project components
become top-level components that nothing depends on, and the metadata component has no bom-ref.
`WithRemoveOrphanedComponents()` throws on a broken tree; adopt the orphans first to anchor it:

```csharp
var settings = new CdxRefineSettings()
    .WithAdoptOrphanedComponents()
    .WithExcludeByName("^xunit(\\..+)?$")
    .WithRemoveOrphanedComponents()
    .WithGroupByName("Test tools", "^xunit");

CdxRefine("./artifacts/deduplicated.cdx.xml", "./artifacts/refined.cdx.xml", settings);
```

Notes:

- A flat SBOM (no `/bom/dependencies` entries) is left alone by adoption and orphan removal;
  both steps just log and skip.
- Only orphans (top-level components with no incoming edge) are ever adopted. A component that
  already has a parent is skipped, not moved.
- An adoption rule's `parent` criteria is only resolved when the rule actually matches a
  component. A rule with a `parent` pattern that matches nothing never throws, because it never
  runs.
- Orphan removal only evaluates top-level components. Nested components
  (`<component><components><component>`) that become unreachable in a hierarchical merge are kept
  with their parent and logged, never pruned on their own.
- In a hierarchical merge, nested package bom-refs are namespaced by their owning project, e.g.
  `Proj@1.0:pkg:nuget/Newtonsoft.Json@13.0.3`. Account for that prefix in `WithAdoptionByBomRef`,
  `WithExcludeByBomRef` and similar bom-ref patterns.

Like `CdxDeduplicate`, `CdxRefine` also takes the SBOM as an XML string or an `XDocument`.

## CdxCliValidate

`CdxCliValidate` validates one or more SBOMs against the CycloneDX schema with `cyclonedx validate`.

```csharp
CdxCliValidate("./artifacts/refined.cdx.xml", new CdxCliValidateSettings
{
    InputFormat = CdxCliValidateInputFormat.Xml,
    FailOnErrors = true,
});
```

By default the CLI only reports validation errors, and the build continues. Set `FailOnErrors = true` to fail the build
on an invalid SBOM. `InputFormat` (`Xml`, `Json` or `AutoDetect`) and `InputVersion` set what to validate against when
the CLI shouldn't detect it.

## Changelog

See [CHANGELOG.md](https://github.com/mgnslndh/Cake.CycloneDX/blob/main/CHANGELOG.md) for the changes in each version.

## License

[MIT](https://github.com/mgnslndh/Cake.CycloneDX/blob/main/LICENSE)
