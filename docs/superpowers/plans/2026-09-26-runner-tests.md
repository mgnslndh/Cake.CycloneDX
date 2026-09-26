# Runner Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the packed Cake.CycloneDX package works on the Cake .NET Tool, Cake.Sdk and Cake Frosting runners, on Cake 6.0.0 and the latest 6.x, on Windows, Linux and macOS.

**Architecture:** A `RunnerTests` Frosting task in `build/` packs the addin, resolves one concrete Cake version, and runs one SBOM pipeline through each runner against a checked-in scenario. Each runner consumes the `.nupkg` from `artifacts/` through a generated `nuget.config`. The task asserts on each runner's final SBOM, checks that the runners' normalized SBOMs are equal, and fails with a per-runner summary. CI runs the task in an OS × Cake-version matrix.

**Tech Stack:** Cake Frosting 6.3.0 (build orchestration), Cake.Tool / Cake.Sdk / Cake.Frosting 6.x (runners under test), CycloneDX .NET tool, CycloneDX CLI v0.30.0, xUnit v3 (addin unit tests), GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-26-runner-tests-design.md`

## Global Constraints

- Test the packed `.nupkg` from `artifacts/`, never a project reference.
- Runners: Cake .NET Tool, Cake.Sdk (file-based only), Cake Frosting.
- Cake versions: `6.0.0` and `6.*` (floating, highest stable 6.x). One concrete version per run, used by every runner.
- The script and `cake.cs` pipelines contain **no `using` directives** for Cake.CycloneDX namespaces.
- CycloneDX CLI version: `v0.30.0` (existing manifest). CI installs the CycloneDX .NET tool `6.1.0`.
- OS matrix: `windows-latest`, `ubuntu-latest`, `macos-latest`, for both pushes to `main` and pull requests.
- No separate test project for `build/`. Each harness check is watched failing once (spec: "Verifying the harness").
- Addin changes follow `AGENTS.md`: braces everywhere (SA1503), XML docs on public API, and validate with `.\build.ps1 --target test`.
- Commit messages use the repository's gitmoji style (`:sparkles: feat: ...`, `:white_check_mark: test: ...`, `:construction_worker: ci: ...`) and end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Facts Established Before Planning

Verified in a scratchpad spike on 2026-09-26. Treat these as known; don't re-investigate.

- In build scripts, types in an alias class's **own** namespace resolve without `using` on both the script runner and Cake.Sdk. Types in **other** namespaces (`...CdxCli.Merge`, `...CdxCli.Validate`) do not.
- `[CakeNamespaceImport]` fixes this for the script runner at assembly, class **or** method level. **Cake.Sdk honors only the method-level attribute**; it ignores class- and assembly-level.
- Cake.Tool 6.0.0 prints 12 `CS8632` warnings (nullable `Settings?` alias parameters). 6.3.0 prints none. This is out of scope.
- `#addin` / `#:sdk` / `#:package` directives cannot read script arguments. The versions must be written into the file, so the task renders a generated copy.
- `dotnet tool install Cake.Tool --version "6.*"` works; Cake.Sdk has versions 6.0.0, 6.1.0, 6.1.1, 6.2.0, 6.3.0.
- Cake.Sdk file-based: run with `dotnet run --no-cache --file cake.cs -- --name=value`. `Argument("name", ...)` works, and a failing task exits with code 1. `--no-cache` prevents reusing a stale build when a locally rebuilt package keeps the same version.
- `ProcessSettings.EnvironmentVariables` is `IDictionary<string, string>`. `CakeNamespaceImportAttribute` has a `Namespace` property.
- Enabling `<ImplicitUsings>` and `<Nullable>` in `build/Build.csproj` builds with 0 warnings.
- End-to-end run of the scenario below (CycloneDX .NET 6.2.0, CLI v0.30.0) produced this final SBOM content:
  - metadata component `bom-ref="Scenario@1.2.3"`, `<group>com.example</group>`, name `Scenario`, version `1.2.3`
  - top-level components exactly: `Newtonsoft.Json` (group `JamesNK`), `App` (type `library`), `Lib` (type `framework`). `Humanizer.Core` is excluded.
  - `<dependency ref="Scenario@1.2.3">` with children `App@1.0.0` and `Lib@1.0.0`
  - `cyclonedx merge` already merges the shared `Newtonsoft.Json`, so `CdxDeduplicate` is a no-op here. It is still called, so the alias loads and runs on every runner.

## Review Focus

1. **Stale local package.** If you rebuild the addin without bumping the version (MinVer gives uncommitted work the same version) and run RunnerTests again, the runners must use the **new** package. Covered by cleaning the run directory and giving every runner process an isolated `NUGET_PACKAGES` (Task 2), plus `--no-cache` for Cake.Sdk (Task 3). Verified in Task 3 Step 7.
2. **Cake version that doesn't exist** (`--cake-version 6.9.9`). The task should fail before running anything, with `Cake version '6.9.9' does not exist on NuGet`. Verified in Task 2 Step 11.
3. **CycloneDX .NET tool not installed.** The task should fail fast with the install command, not a runner-level "could not locate executable". Verified in Task 2 Step 12.
4. **One runner fails.** The other runners still run, and the summary names every failing runner. Verified in Task 3 Step 6 (the Cake.Sdk pipeline is deliberately broken while the script runner passes).
5. **Paths with spaces** (for example a clone under `C:\My Projects\`). All paths passed to child processes are quoted (`AppendQuoted` / `AppendSwitchQuoted`). Verified in Task 4 Step 6 by running from a directory whose path contains a space.

---

### Task 1: Import settings namespaces on every alias that needs them

The script runner and Cake.Sdk fail to compile `new CdxCliMergeSettings()` and `new CdxCliValidateSettings()` without `using` directives. Add method-level `[CakeNamespaceImport]`, which is the only placement Cake.Sdk honors, and guard the whole assembly with a reflection test.

**Files:**
- Create: `src/Cake.CycloneDX.Tests/Unit/AliasNamespaceImportTests.cs`
- Modify: `src/Cake.CycloneDX/Tools/CdxCli/CdxCliAliases.Merge.cs` (attribute on `CdxCliMerge`)
- Modify: `src/Cake.CycloneDX/Tools/CdxCli/CdxCliAliases.Validate.cs` (attribute on both `CdxCliValidate` overloads)
- Modify: `AGENTS.md` (one bullet under "Cake Addin Guidelines")

**Interfaces:**
- Consumes: nothing.
- Produces: the published addin exposes `Cake.CycloneDX.Tools.CdxCli.Merge` and `...CdxCli.Validate` types to build scripts without `using` directives. Tasks 2–4 rely on this.

- [ ] **Step 1: Write the failing test**

Create `src/Cake.CycloneDX.Tests/Unit/AliasNamespaceImportTests.cs`:

```csharp
using System.Reflection;
using Cake.Core.Annotations;
using Cake.CycloneDX.Tools.CdxCli;
using Xunit;

namespace Cake.CycloneDX.Tests.Unit
{
    public sealed class AliasNamespaceImportTests
    {
        // Cake.Sdk only honors [CakeNamespaceImport] on the alias method itself, and build scripts only get the
        // alias class's own namespace for free. Every addin type an alias takes must therefore live in the alias
        // class's namespace or be imported on the method.
        [Fact]
        public void Alias_Parameter_Types_From_Other_Namespaces_Are_Imported_On_The_Alias_Method()
        {
            var assembly = typeof(CdxCliAliases).Assembly;
            var violations = new List<string>();

            var aliases = assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.IsDefined(typeof(CakeMethodAliasAttribute), false));

            foreach (var alias in aliases)
            {
                var imported = alias.GetCustomAttributes<CakeNamespaceImportAttribute>()
                    .Select(attribute => attribute.Namespace)
                    .Append(alias.DeclaringType.Namespace)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var parameter in alias.GetParameters())
                {
                    var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                    if (type.Assembly == assembly && !imported.Contains(type.Namespace))
                    {
                        violations.Add($"{alias.DeclaringType.Name}.{alias.Name}({type.Name}) needs [CakeNamespaceImport(\"{type.Namespace}\")]");
                    }
                }
            }

            Assert.Empty(violations);
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails for the right reason**

Run: `dotnet test --project src/Cake.CycloneDX.Tests -f net10.0 -- --filter-class "*AliasNamespaceImportTests*"`
Expected: FAIL. `Assert.Empty()` lists three violations: `CdxCliAliases.CdxCliMerge(CdxCliMergeSettings) needs [CakeNamespaceImport("Cake.CycloneDX.Tools.CdxCli.Merge")]` and two `CdxCliValidate(CdxCliValidateSettings)` entries for `...CdxCli.Validate`.

- [ ] **Step 3: Add the method-level imports**

In `src/Cake.CycloneDX/Tools/CdxCli/CdxCliAliases.Merge.cs`, directly under `[CakeMethodAlias]` on `CdxCliMerge`:

```csharp
    [CakeMethodAlias]
    [CakeNamespaceImport("Cake.CycloneDX.Tools.CdxCli.Merge")]
    public static void CdxCliMerge(this ICakeContext context, FilePathCollection inputFilePaths, FilePath outputFilePath, CdxCliMergeSettings? settings = null)
```

In `src/Cake.CycloneDX/Tools/CdxCli/CdxCliAliases.Validate.cs`, on **both** overloads:

```csharp
    [CakeMethodAlias]
    [CakeNamespaceImport("Cake.CycloneDX.Tools.CdxCli.Validate")]
    public static void CdxCliValidate(this ICakeContext context, FilePathCollection inputFilePaths, CdxCliValidateSettings? settings = null)
```

```csharp
    [CakeMethodAlias]
    [CakeNamespaceImport("Cake.CycloneDX.Tools.CdxCli.Validate")]
    public static void CdxCliValidate(this ICakeContext context, FilePath inputFilePath, CdxCliValidateSettings? settings = null)
```

`Cake.Core.Annotations` is already imported in both files.

- [ ] **Step 4: Run the test and confirm it passes**

Run: `dotnet test --project src/Cake.CycloneDX.Tests -f net10.0 -- --filter-class "*AliasNamespaceImportTests*"`
Expected: PASS (1 test).

- [ ] **Step 5: Document the rule in AGENTS.md**

In `AGENTS.md`, under "How this repository applies them:", replace the **New aliases** bullet with:

```markdown
- **New aliases:** put them in a `static` class ending in `Aliases`, marked
  `[CakeAliasCategory("CycloneDX")]`, as `ICakeContext` extension methods marked `[CakeMethodAlias]`.
  If an alias takes a type from a namespace other than its class's, add
  `[CakeNamespaceImport("<namespace>")]` **on the method**. Cake.Sdk ignores class- and
  assembly-level imports. `AliasNamespaceImportTests` enforces this.
```

- [ ] **Step 6: Run the full build and tests**

Run: `.\build.ps1 --target test`
Expected: `0 Warning(s)`, `0 Error(s)`, all tests pass (397).

- [ ] **Step 7: Commit**

```bash
git add src/Cake.CycloneDX.Tests/Unit/AliasNamespaceImportTests.cs src/Cake.CycloneDX/Tools/CdxCli/CdxCliAliases.Merge.cs src/Cake.CycloneDX/Tools/CdxCli/CdxCliAliases.Validate.cs AGENTS.md
git commit -m ":bug: fix: Import CdxCli settings namespaces on the aliases" -m "Build scripts could not use CdxCliMergeSettings or CdxCliValidateSettings without using directives, on both the Cake .NET Tool and Cake.Sdk. Import the namespaces on the alias methods; Cake.Sdk ignores class- and assembly-level imports." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: RunnerTests task with the Cake .NET Tool runner

Build the harness end to end with one runner: the scenario fixture, version resolution, prerequisites, process isolation, script rendering, assertions, output comparison and the summary. Later tasks only add runners.

**Files:**
- Create: `tests/runners/scenario/App/App.csproj`
- Create: `tests/runners/scenario/Lib/Lib.csproj`
- Create: `tests/runners/script/build.cake`
- Create: `build/RunnerTests/RunnerTestContext.cs`
- Create: `build/RunnerTests/IRunner.cs`
- Create: `build/RunnerTests/RunnerResult.cs`
- Create: `build/RunnerTests/RunnerProcess.cs`
- Create: `build/RunnerTests/CakeVersionResolver.cs`
- Create: `build/RunnerTests/NuGetConfig.cs`
- Create: `build/RunnerTests/ScriptTemplate.cs`
- Create: `build/RunnerTests/SbomAssertions.cs`
- Create: `build/RunnerTests/SbomNormalizer.cs`
- Create: `build/RunnerTests/ScriptRunner.cs`
- Create: `build/Tasks/RunnerTestsTask.cs`
- Move: `src/Cake.CycloneDX.Dogfooding.Build/Tools/*.cs` → `build/Tools/` (copy now; Task 4 deletes the originals)
- Modify: `build/Build.csproj` (add `ImplicitUsings`, `Nullable`)

**Interfaces:**
- Consumes: Task 1's namespace imports (the script has no `using` directives).
- Produces, for Tasks 3–4:
  - `internal interface IRunner { string Name { get; } int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory); }`
  - `internal sealed record RunnerTestContext(string CakeVersion, string AddinVersion, DirectoryPath RepositoryRoot, DirectoryPath RunDirectory, DirectoryPath NuGetPackagesDirectory, DirectoryPath CycloneDxCliDirectory)` with `RunnersDirectory`, `ScenarioDirectory`, `ArtifactsDirectory`
  - `RunnerProcess.Run(ICakeContext, RunnerTestContext, FilePath executable, ProcessArgumentBuilder arguments, DirectoryPath workingDirectory) : int`
  - `NuGetConfig.Write(DirectoryPath directory, DirectoryPath localFeed) : FilePath`
  - `ScriptTemplate.Render(FilePath source, FilePath destination, IReadOnlyDictionary<string, string> replacements)`, `ScriptTemplate.GetPipeline(FilePath source) : string`, `ScriptTemplate.PipelineMarker`
  - `RunnerTestsTask.Runners` (a `static readonly IRunner[]`) that later tasks append to
  - Each runner writes its final SBOM to `<outputDirectory>/refined.cdx.xml`
  - Runner command-line contract: `--scenario=<absolute dir>` and `--output=<absolute dir>`

- [ ] **Step 1: Create the scenario fixture**

`tests/runners/scenario/Lib/Lib.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>

</Project>
```

`tests/runners/scenario/App/App.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Lib/Lib.csproj" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Humanizer.Core" Version="2.14.1" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the script runner pipeline**

`tests/runners/script/build.cake`. The `#addin` version is rewritten by `ScriptTemplate` at run time. Keep everything after the marker line **byte-identical** to `tests/runners/sdk/cake.cs` (Task 3); `RunnerTestsTask` enforces this once both files exist.

```csharp
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
```

- [ ] **Step 3: Enable implicit usings and nullable in the Build project**

In `build/Build.csproj`, change the first `PropertyGroup` to:

```xml
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RunWorkingDirectory>$(MSBuildProjectDirectory)\..</RunWorkingDirectory>
  </PropertyGroup>
```

- [ ] **Step 4: Copy the CycloneDX CLI downloader into the Build project**

```bash
mkdir -p build/Tools
cp src/Cake.CycloneDX.Dogfooding.Build/Tools/*.cs build/Tools/
sed -i 's/^namespace Cake\.CycloneDX\.Dogfooding\.Build\.Tools;/namespace Build.Tools;/' build/Tools/*.cs
grep -h "^namespace" build/Tools/*.cs | sort -u
```

Expected output: exactly `namespace Build.Tools;`. The copies stay `internal`; the Build project is one assembly.

- [ ] **Step 5: Add the harness building blocks**

`build/RunnerTests/RunnerTestContext.cs`:

```csharp
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Everything a runner needs to know about the current runner-test run.
/// </summary>
internal sealed record RunnerTestContext(
    string CakeVersion,
    string AddinVersion,
    DirectoryPath RepositoryRoot,
    DirectoryPath RunDirectory,
    DirectoryPath NuGetPackagesDirectory,
    DirectoryPath CycloneDxCliDirectory)
{
    public DirectoryPath RunnersDirectory => RepositoryRoot.Combine("tests/runners");

    public DirectoryPath ScenarioDirectory => RunnersDirectory.Combine("scenario");

    public DirectoryPath ArtifactsDirectory => RepositoryRoot.Combine("artifacts");
}
```

`build/RunnerTests/IRunner.cs`:

```csharp
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// A Cake runner under test. Runs the scenario pipeline and writes <c>refined.cdx.xml</c> to the output directory.
/// </summary>
internal interface IRunner
{
    string Name { get; }

    /// <returns>The exit code of the runner; 0 means success.</returns>
    int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory);
}
```

`build/RunnerTests/RunnerResult.cs`:

```csharp
using System.Xml.Linq;

namespace Build.RunnerTests;

internal sealed class RunnerResult
{
    public RunnerResult(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public List<string> Failures { get; } = new();

    public XDocument? Sbom { get; set; }

    public bool Passed => Failures.Count == 0;
}
```

`build/RunnerTests/RunnerProcess.cs`:

```csharp
using Cake.Common;
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Starts runner processes with the CycloneDX CLI on the PATH and an isolated NuGet package folder, so a locally
/// rebuilt addin that keeps its version is never served from a stale cache.
/// </summary>
internal static class RunnerProcess
{
    public static int Run(
        ICakeContext context,
        RunnerTestContext test,
        FilePath executable,
        ProcessArgumentBuilder arguments,
        DirectoryPath workingDirectory)
    {
        var path = test.CycloneDxCliDirectory.FullPath
            + System.IO.Path.PathSeparator
            + (Environment.GetEnvironmentVariable("PATH") ?? string.Empty);

        return context.StartProcess(executable, new ProcessSettings
        {
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            EnvironmentVariables = new Dictionary<string, string>
            {
                ["PATH"] = path,
                ["NUGET_PACKAGES"] = test.NuGetPackagesDirectory.FullPath,
            },
        });
    }
}
```

`build/RunnerTests/CakeVersionResolver.cs`:

```csharp
using System.Text.Json;
using Cake.Core;

namespace Build.RunnerTests;

/// <summary>
/// Resolves a requested Cake version such as <c>6.0.0</c> or <c>6.*</c> to one concrete, published stable version.
/// </summary>
internal static class CakeVersionResolver
{
    private const string IndexUrl = "https://api.nuget.org/v3-flatcontainer/cake.tool/index.json";

    public static string Resolve(string requested)
    {
        using var client = new HttpClient();
        var json = client.GetStringAsync(IndexUrl).GetAwaiter().GetResult();
        var published = JsonDocument.Parse(json).RootElement.GetProperty("versions")
            .EnumerateArray()
            .Select(element => element.GetString()!)
            .Where(version => !version.Contains('-'))
            .ToList();

        if (!requested.EndsWith('*'))
        {
            return published.Contains(requested, StringComparer.Ordinal)
                ? requested
                : throw new CakeException($"Cake version '{requested}' does not exist on NuGet.");
        }

        var prefix = requested[..^1];
        var match = published
            .Where(version => version.StartsWith(prefix, StringComparison.Ordinal))
            .Select(Version.Parse)
            .DefaultIfEmpty()
            .Max();

        return match?.ToString() ?? throw new CakeException($"No stable Cake version matches '{requested}'.");
    }
}
```

`build/RunnerTests/NuGetConfig.cs`:

```csharp
using System.Xml.Linq;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Writes a nuget.config whose sources are the local artifacts feed and nuget.org.
/// </summary>
internal static class NuGetConfig
{
    public static FilePath Write(DirectoryPath directory, DirectoryPath localFeed)
    {
        System.IO.Directory.CreateDirectory(directory.FullPath);
        var path = directory.CombineWithFilePath("nuget.config");

        new XDocument(
            new XElement(
                "configuration",
                new XElement(
                    "packageSources",
                    new XElement("clear"),
                    new XElement("add", new XAttribute("key", "local"), new XAttribute("value", localFeed.FullPath)),
                    new XElement("add", new XAttribute("key", "nuget.org"), new XAttribute("value", "https://api.nuget.org/v3/index.json")))))
            .Save(path.FullPath);

        return path;
    }
}
```

`build/RunnerTests/ScriptTemplate.cs`:

```csharp
using System.Text.RegularExpressions;
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Renders runner scripts whose package directives must carry literal versions.
/// </summary>
internal static class ScriptTemplate
{
    public const string PipelineMarker = "// --- pipeline ---";

    /// <param name="replacements">Regex pattern (matched per line) → replacement line.</param>
    public static void Render(FilePath source, FilePath destination, IReadOnlyDictionary<string, string> replacements)
    {
        var text = File.ReadAllText(source.FullPath);

        foreach (var (pattern, replacement) in replacements)
        {
            var regex = new Regex(pattern, RegexOptions.Multiline);
            if (!regex.IsMatch(text))
            {
                throw new CakeException($"'{source.FullPath}' has no line matching '{pattern}'.");
            }

            text = regex.Replace(text, replacement);
        }

        System.IO.Directory.CreateDirectory(destination.GetDirectory().FullPath);
        File.WriteAllText(destination.FullPath, text);
    }

    /// <returns>The text after <see cref="PipelineMarker"/>, with normalized line endings.</returns>
    public static string GetPipeline(FilePath source)
    {
        var text = File.ReadAllText(source.FullPath).Replace("\r\n", "\n");
        var index = text.IndexOf(PipelineMarker, StringComparison.Ordinal);
        return index < 0
            ? throw new CakeException($"'{source.FullPath}' has no '{PipelineMarker}' line.")
            : text[index..];
    }
}
```

`build/RunnerTests/SbomAssertions.cs`:

```csharp
using System.Xml.Linq;

namespace Build.RunnerTests;

/// <summary>
/// Checks a runner's final SBOM against what the scenario pipeline must produce.
/// </summary>
internal static class SbomAssertions
{
    public static IReadOnlyList<string> Check(XDocument sbom)
    {
        var failures = new List<string>();
        var root = sbom.Root!;
        var ns = root.GetDefaultNamespace();

        var metadata = root.Element(ns + "metadata")?.Element(ns + "component");
        Expect(failures, "metadata component group", "com.example", metadata?.Element(ns + "group")?.Value);
        Expect(failures, "metadata component name", "Scenario", metadata?.Element(ns + "name")?.Value);
        Expect(failures, "metadata component version", "1.2.3", metadata?.Element(ns + "version")?.Value);

        var components = root.Element(ns + "components")?.Elements(ns + "component").ToList() ?? new List<XElement>();
        var names = components
            .Select(component => component.Element(ns + "name")?.Value)
            .OrderBy(name => name, StringComparer.Ordinal);
        Expect(failures, "top-level components", "App, Lib, Newtonsoft.Json", string.Join(", ", names));

        var newtonsoft = components.FirstOrDefault(component => component.Element(ns + "name")?.Value == "Newtonsoft.Json");
        Expect(failures, "Newtonsoft.Json group", "JamesNK", newtonsoft?.Element(ns + "group")?.Value);

        var lib = components.FirstOrDefault(component => component.Element(ns + "name")?.Value == "Lib");
        Expect(failures, "Lib type", "framework", lib?.Attribute("type")?.Value);

        var scenarioDependencies = root.Element(ns + "dependencies")?
            .Elements(ns + "dependency")
            .FirstOrDefault(dependency => dependency.Attribute("ref")?.Value == "Scenario@1.2.3")?
            .Elements(ns + "dependency")
            .Select(dependency => dependency.Attribute("ref")?.Value)
            .OrderBy(reference => reference, StringComparer.Ordinal);
        Expect(
            failures,
            "Scenario@1.2.3 dependencies",
            "App@1.0.0, Lib@1.0.0",
            scenarioDependencies is null ? null : string.Join(", ", scenarioDependencies));

        return failures;
    }

    private static void Expect(List<string> failures, string what, string expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            failures.Add($"{what}: expected '{expected}' but was '{actual ?? "<missing>"}'");
        }
    }
}
```

`build/RunnerTests/SbomNormalizer.cs`:

```csharp
using System.Xml.Linq;

namespace Build.RunnerTests;

/// <summary>
/// Removes run-specific values from an SBOM and sorts it, so SBOMs from different runners can be compared.
/// </summary>
internal static class SbomNormalizer
{
    public static XDocument Normalize(XDocument sbom)
    {
        var copy = new XDocument(sbom);
        var root = copy.Root!;
        var ns = root.GetDefaultNamespace();

        root.Attribute("serialNumber")?.Remove();
        var metadata = root.Element(ns + "metadata");
        metadata?.Element(ns + "timestamp")?.Remove();
        metadata?.Element(ns + "tools")?.Remove();

        SortChildren(root.Element(ns + "components"), element => element.Attribute("bom-ref")?.Value);

        var dependencies = root.Element(ns + "dependencies");
        if (dependencies is not null)
        {
            foreach (var dependency in dependencies.Elements(ns + "dependency"))
            {
                SortChildren(dependency, element => element.Attribute("ref")?.Value);
            }

            SortChildren(dependencies, element => element.Attribute("ref")?.Value);
        }

        return copy;
    }

    /// <returns><see langword="null"/> when equal; otherwise the first differing line of the normalized SBOMs.</returns>
    public static string? FindFirstDifference(XDocument expected, XDocument actual)
    {
        var expectedLines = Normalize(expected).ToString().Split('\n');
        var actualLines = Normalize(actual).ToString().Split('\n');

        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var left = i < expectedLines.Length ? expectedLines[i].Trim() : "<end>";
            var right = i < actualLines.Length ? actualLines[i].Trim() : "<end>";
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                return $"line {i + 1}: '{left}' vs '{right}'";
            }
        }

        return null;
    }

    private static void SortChildren(XElement? parent, Func<XElement, string?> key)
    {
        if (parent is null)
        {
            return;
        }

        var sorted = parent.Elements().OrderBy(element => key(element) ?? string.Empty, StringComparer.Ordinal).ToList();
        parent.RemoveNodes();
        parent.Add(sorted);
    }
}
```

- [ ] **Step 6: Add the script runner**

`build/RunnerTests/ScriptRunner.cs`:

```csharp
using Cake.Common;
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Runs <c>tests/runners/script/build.cake</c> with the Cake .NET Tool at the requested Cake version.
/// </summary>
internal sealed class ScriptRunner : IRunner
{
    public string Name => "script";

    public int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory)
    {
        var toolDirectory = workDirectory.Combine("cake-tool");
        var install = context.StartProcess("dotnet", new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .Append("tool")
                .Append("install")
                .Append("Cake.Tool")
                .AppendSwitch("--version", test.CakeVersion)
                .AppendSwitchQuoted("--tool-path", toolDirectory.FullPath),
        });
        if (install != 0)
        {
            return install;
        }

        var sourceDirectory = workDirectory.Combine("src");
        NuGetConfig.Write(sourceDirectory, test.ArtifactsDirectory);

        var script = sourceDirectory.CombineWithFilePath("build.cake");
        ScriptTemplate.Render(
            test.RunnersDirectory.CombineWithFilePath("script/build.cake"),
            script,
            new Dictionary<string, string>
            {
                [@"^#addin nuget:\?package=Cake\.CycloneDX&version=[^\r\n]*"] =
                    $"#addin nuget:?package=Cake.CycloneDX&version={test.AddinVersion}",
            });

        var executable = toolDirectory.CombineWithFilePath(context.IsRunningOnWindows() ? "dotnet-cake.exe" : "dotnet-cake");
        return RunnerProcess.Run(
            context,
            test,
            executable,
            new ProcessArgumentBuilder()
                .AppendQuoted(script.FullPath)
                .AppendSwitchQuoted("--scenario", "=", test.ScenarioDirectory.FullPath)
                .AppendSwitchQuoted("--output", "=", outputDirectory.FullPath),
            sourceDirectory);
    }
}
```

- [ ] **Step 7: Add the RunnerTests task**

`build/Tasks/RunnerTestsTask.cs`:

```csharp
using System.Xml.Linq;
using Build.RunnerTests;
using Build.Tools;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Tasks;

/// <summary>
/// Runs the scenario pipeline on every Cake runner against the packed addin. Usage:
/// <c>.\build.ps1 --target RunnerTests [--cake-version 6.0.0|6.*]</c>.
/// </summary>
[TaskName("RunnerTests")]
[IsDependentOn(typeof(PackTask))]
public sealed class RunnerTestsTask : FrostingTask<BuildContext>
{
    private const string CycloneDxCliVersion = "v0.30.0";

    private static readonly IRunner[] Runners =
    [
        new ScriptRunner(),
    ];

    public override void Run(BuildContext context)
    {
        var cakeVersion = CakeVersionResolver.Resolve(context.Argument("cake-version", "6.*"));
        var addinVersion = ThisAssembly.PackageVersion;
        var root = context.Environment.WorkingDirectory;

        var package = root.CombineWithFilePath($"artifacts/Cake.CycloneDX.{addinVersion}.nupkg");
        if (!context.FileExists(package))
        {
            throw new CakeException($"Package '{package.FullPath}' was not found. Run the Pack target first.");
        }

        context.Information("Runner tests: Cake {0}, Cake.CycloneDX {1}", cakeVersion, addinVersion);

        EnsureCycloneDxDotNetTool(context);
        new CycloneDxCliDownloader(new CycloneDxReleaseManifestResolver()).Download(context, CycloneDxCliVersion);
        var cliDirectory = new DirectoryPath(context.Configuration.GetValue("Paths_Tools")).MakeAbsolute(context.Environment);

        var runDirectory = root.Combine($"artifacts/runner-tests/{cakeVersion}");
        context.EnsureDirectoryExists(runDirectory);
        context.CleanDirectory(runDirectory);

        var test = new RunnerTestContext(
            cakeVersion,
            addinVersion,
            root,
            runDirectory,
            runDirectory.Combine("nuget-packages"),
            cliDirectory);

        var results = Runners.Select(runner => RunOne(context, test, runner)).ToList();
        CompareOutputs(results);
        Report(context, results);
    }

    private static RunnerResult RunOne(ICakeContext context, RunnerTestContext test, IRunner runner)
    {
        var result = new RunnerResult(runner.Name);
        var workDirectory = test.RunDirectory.Combine(runner.Name);
        var outputDirectory = workDirectory.Combine("out");
        context.EnsureDirectoryExists(outputDirectory);

        context.Information("=== Runner: {0} ===", runner.Name);
        int exitCode;
        try
        {
            exitCode = runner.Run(context, test, workDirectory, outputDirectory);
        }
        catch (Exception exception)
        {
            result.Failures.Add($"runner threw: {exception.Message}");
            return result;
        }

        if (exitCode != 0)
        {
            result.Failures.Add($"exited with code {exitCode}");
            return result;
        }

        var sbomFile = outputDirectory.CombineWithFilePath("refined.cdx.xml");
        if (!context.FileExists(sbomFile))
        {
            result.Failures.Add($"'{sbomFile.FullPath}' was not produced");
            return result;
        }

        result.Sbom = XDocument.Load(sbomFile.FullPath);
        result.Failures.AddRange(SbomAssertions.Check(result.Sbom));
        return result;
    }

    private static void CompareOutputs(IReadOnlyList<RunnerResult> results)
    {
        var reference = results.FirstOrDefault(result => result.Sbom is not null);
        if (reference is null)
        {
            return;
        }

        foreach (var result in results.Where(result => result.Sbom is not null && result != reference))
        {
            var difference = SbomNormalizer.FindFirstDifference(reference.Sbom!, result.Sbom!);
            if (difference is not null)
            {
                result.Failures.Add($"SBOM differs from the {reference.Name} runner's at {difference}");
            }
        }
    }

    private static void Report(ICakeContext context, IReadOnlyList<RunnerResult> results)
    {
        context.Information("=== Runner test summary ===");
        foreach (var result in results)
        {
            if (result.Passed)
            {
                context.Information("  {0,-10} passed", result.Name);
                continue;
            }

            context.Error("  {0,-10} FAILED", result.Name);
            foreach (var failure in result.Failures)
            {
                context.Error("      {0}", failure);
            }
        }

        if (results.Any(result => !result.Passed))
        {
            throw new CakeException("Runner tests failed. See the summary above.");
        }
    }

    private static void EnsureCycloneDxDotNetTool(ICakeContext context)
    {
        int exitCode;
        try
        {
            exitCode = context.StartProcess("dotnet-CycloneDX", new ProcessSettings { Arguments = "--version" });
        }
        catch (Exception)
        {
            exitCode = -1;
        }

        if (exitCode != 0)
        {
            throw new CakeException("The CycloneDX .NET tool is not installed. Install it with: dotnet tool install -g CycloneDX");
        }
    }
}
```

- [ ] **Step 8: Build the Build project**

Run: `dotnet build build/Build.csproj`
Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 9: Run the runner tests on the latest Cake 6.x**

Run: `.\build.ps1 --target RunnerTests`
Expected: logs `Runner tests: Cake 6.3.0, Cake.CycloneDX <version>` (or a newer 6.x), the script runner's pipeline output ending in `BOM validated successfully.`, and a summary with `script     passed`. `artifacts/runner-tests/6.3.0/script/out/refined.cdx.xml` exists.

If the `#addin` line cannot find the package: confirm `artifacts/runner-tests/<version>/script/src/nuget.config` lists the `artifacts` folder as `local`, and that `artifacts/Cake.CycloneDX.<version>.nupkg` exists.

- [ ] **Step 10: Run the runner tests on Cake 6.0.0**

Run: `.\build.ps1 --target RunnerTests --cake-version 6.0.0`
Expected: `Runner tests: Cake 6.0.0, ...`, `script     passed`. The 12 `CS8632` warnings from Cake 6.0.0 are expected (see Facts).

- [ ] **Step 11: Watch the version check fail**

Run: `.\build.ps1 --target RunnerTests --cake-version 6.9.9`
Expected: the run fails before any runner starts, with `Cake version '6.9.9' does not exist on NuGet.`

- [ ] **Step 12: Watch the prerequisite check fail**

Temporarily change the checked executable in `EnsureCycloneDxDotNetTool` from `"dotnet-CycloneDX"` to `"dotnet-CycloneDX-missing"`, then run `.\build.ps1 --target RunnerTests`.
Expected: fails fast with `The CycloneDX .NET tool is not installed. Install it with: dotnet tool install -g CycloneDX`, before any runner starts. **Revert the change.**

- [ ] **Step 13: Watch an assertion fail**

Temporarily change `"App, Lib, Newtonsoft.Json"` in `SbomAssertions.Check` to `"App, Lib"`, then run `.\build.ps1 --target RunnerTests`.
Expected: summary shows `script     FAILED` with `top-level components: expected 'App, Lib' but was 'App, Lib, Newtonsoft.Json'`, and the task fails. **Revert the change.**

- [ ] **Step 14: Commit**

```bash
git add tests/runners build/Build.csproj build/Tools build/RunnerTests build/Tasks/RunnerTestsTask.cs
git commit -m ":white_check_mark: test: Add RunnerTests task with the Cake .NET Tool runner" -m "Packs the addin and runs an SBOM pipeline scenario through a build.cake script on a chosen Cake version, consuming the package from a local feed. Asserts on the final SBOM and prints a per-runner summary." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Cake.Sdk runner and cross-runner comparison

**Files:**
- Create: `tests/runners/sdk/cake.cs`
- Create: `build/RunnerTests/SdkRunner.cs`
- Modify: `build/Tasks/RunnerTestsTask.cs` (register the runner; enforce identical pipelines)

**Interfaces:**
- Consumes: `IRunner`, `RunnerTestContext`, `RunnerProcess.Run`, `NuGetConfig.Write`, `ScriptTemplate.Render`, `ScriptTemplate.GetPipeline`, `RunnerTestsTask.Runners` (Task 2).
- Produces: `SdkRunner` (`Name` = `"sdk"`); the pipeline-equality check.

- [ ] **Step 1: Create the Cake.Sdk pipeline**

`tests/runners/sdk/cake.cs`. The two directive versions are rewritten at run time. Everything from `// --- pipeline ---` on must be **byte-identical** to `tests/runners/script/build.cake`; copy it from there.

```csharp
// Cake.Sdk runner test. RunnerTestsTask renders a copy with the real Cake.Sdk and addin versions.
#:sdk Cake.Sdk@6.0.0
#:package Cake.CycloneDX@0.0.0

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
```

- [ ] **Step 2: Add the Cake.Sdk runner**

`build/RunnerTests/SdkRunner.cs`:

```csharp
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Runs <c>tests/runners/sdk/cake.cs</c> as a Cake.Sdk file-based app at the requested Cake version.
/// </summary>
internal sealed class SdkRunner : IRunner
{
    public string Name => "sdk";

    public int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory)
    {
        var sourceDirectory = workDirectory.Combine("src");
        NuGetConfig.Write(sourceDirectory, test.ArtifactsDirectory);

        var file = sourceDirectory.CombineWithFilePath("cake.cs");
        ScriptTemplate.Render(
            test.RunnersDirectory.CombineWithFilePath("sdk/cake.cs"),
            file,
            new Dictionary<string, string>
            {
                [@"^#:sdk Cake\.Sdk@[^\r\n]*"] = $"#:sdk Cake.Sdk@{test.CakeVersion}",
                [@"^#:package Cake\.CycloneDX@[^\r\n]*"] = $"#:package Cake.CycloneDX@{test.AddinVersion}",
            });

        return RunnerProcess.Run(
            context,
            test,
            "dotnet",
            new ProcessArgumentBuilder()
                .Append("run")
                .Append("--no-cache")
                .AppendSwitchQuoted("--file", file.FullPath)
                .Append("--")
                .AppendSwitchQuoted("--scenario", "=", test.ScenarioDirectory.FullPath)
                .AppendSwitchQuoted("--output", "=", outputDirectory.FullPath),
            sourceDirectory);
    }
}
```

- [ ] **Step 3: Register the runner and enforce identical pipelines**

In `build/Tasks/RunnerTestsTask.cs`, change `Runners` to:

```csharp
    private static readonly IRunner[] Runners =
    [
        new ScriptRunner(),
        new SdkRunner(),
    ];
```

and add this as the first line of `Run`, before resolving the Cake version:

```csharp
        EnsureIdenticalScriptPipelines(context);
```

Add the method to the class:

```csharp
    private static void EnsureIdenticalScriptPipelines(ICakeContext context)
    {
        var runners = context.Environment.WorkingDirectory.Combine("tests/runners");
        var script = ScriptTemplate.GetPipeline(runners.CombineWithFilePath("script/build.cake"));
        var sdk = ScriptTemplate.GetPipeline(runners.CombineWithFilePath("sdk/cake.cs"));
        if (!string.Equals(script, sdk, StringComparison.Ordinal))
        {
            throw new CakeException(
                "The pipelines in tests/runners/script/build.cake and tests/runners/sdk/cake.cs differ. "
                + $"Everything after '{ScriptTemplate.PipelineMarker}' must be identical.");
        }
    }
```

- [ ] **Step 4: Run the runner tests on the latest Cake 6.x**

Run: `dotnet build build/Build.csproj` (expect 0 warnings), then `.\build.ps1 --target RunnerTests`
Expected: summary shows `script     passed` and `sdk        passed`.

- [ ] **Step 5: Run on Cake 6.0.0**

Run: `.\build.ps1 --target RunnerTests --cake-version 6.0.0`
Expected: both runners pass.

- [ ] **Step 6: Watch the cross-runner comparison fail, and the other runner still run**

The pipeline-equality guard (Step 3) would stop this experiment before any runner starts. So **also** comment out the `EnsureIdenticalScriptPipelines(context);` line for this step. Then in `tests/runners/sdk/cake.cs` change `.WithTypeByName("framework", "^Lib$")` to `.WithTypeByName("platform", "^Lib$")`, and run `.\build.ps1 --target RunnerTests`.
Expected: summary shows `script     passed` and `sdk        FAILED` with **two** failures: `Lib type: expected 'framework' but was 'platform'`, and `SBOM differs from the script runner's at line <n>: '<component type="framework" bom-ref="Lib@1.0.0">' vs '<component type="platform" bom-ref="Lib@1.0.0">'`. The task fails.

Now uncomment the `EnsureIdenticalScriptPipelines(context);` line, leave the `platform` edit in `cake.cs`, and run again.
Expected: fails immediately, before any runner starts, with `The pipelines in tests/runners/script/build.cake and tests/runners/sdk/cake.cs differ.` **Revert the `platform` edit** so `cake.cs` matches `build.cake` again.

- [ ] **Step 7: Confirm a rebuilt package with the same version is picked up (Review Focus 1)**

Make a temporary behavior change that shows up in the SBOM without changing the package version. In `src/Cake.CycloneDX/Tools/CdxRefine/CdxRefineAliases.cs`, in `AssignGroup`, change `groupElement.Value = groupName;` to `groupElement.Value = groupName + "-stale-check";`. **Don't commit.** Run `.\build.ps1 --target RunnerTests`.
Expected: both runners fail with `Newtonsoft.Json group: expected 'JamesNK' but was 'JamesNK-stale-check'`, which proves the freshly packed package was used even though its version did not change. **Revert the change**, run `.\build.ps1 --target RunnerTests` again, and expect both runners to pass. That proves the revert was picked up too.

- [ ] **Step 8: Commit**

```bash
git add tests/runners/sdk/cake.cs build/RunnerTests/SdkRunner.cs build/Tasks/RunnerTestsTask.cs
git commit -m ":white_check_mark: test: Add the Cake.Sdk runner to RunnerTests" -m "Runs the same pipeline as a Cake.Sdk file-based app, compares its SBOM with the script runner's, and fails if the two script pipelines drift apart." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Frosting runner, replacing the dogfooding project

**Files:**
- Create: `tests/runners/frosting/Frosting.csproj`
- Create: `tests/runners/frosting/Program.cs`
- Create: `tests/runners/frosting/ScenarioContext.cs`
- Create: `tests/runners/frosting/DefaultTask.cs`
- Create: `build/RunnerTests/FrostingRunner.cs`
- Modify: `build/Tasks/RunnerTestsTask.cs` (register the runner)
- Modify: `build/Tasks/AllTask.cs` (drop `DogfoodTask`)
- Modify: `src/Cake.CycloneDX.sln` (remove the dogfooding project)
- Delete: `src/Cake.CycloneDX.Dogfooding.Build/` (whole folder), `build/Tasks/DogfoodTask.cs`, `RunDogfooding.ps1`

**Interfaces:**
- Consumes: `IRunner`, `RunnerTestContext`, `RunnerProcess.Run`, `NuGetConfig.Write`, `RunnerTestsTask.Runners` (Task 2).
- Produces: `FrostingRunner` (`Name` = `"frosting"`).

- [ ] **Step 1: Create the Frosting project**

`tests/runners/frosting/Frosting.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- RunnerTestsTask sets both; the defaults let the project open and restore on its own. -->
    <CakeVersion Condition="'$(CakeVersion)' == ''">6.*</CakeVersion>
    <AddinVersion Condition="'$(AddinVersion)' == ''">*-*</AddinVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Cake.Frosting" Version="$(CakeVersion)" />
    <PackageReference Include="Cake.CycloneDX" Version="$(AddinVersion)" />
  </ItemGroup>

</Project>
```

`tests/runners/frosting/Program.cs`:

```csharp
using Cake.Frosting;

return new CakeHost()
    .UseContext<Frosting.ScenarioContext>()
    .Run(args);
```

`tests/runners/frosting/ScenarioContext.cs`:

```csharp
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
```

`tests/runners/frosting/DefaultTask.cs`. It runs the same steps as the script pipeline in Frosting form:

```csharp
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
```

- [ ] **Step 2: Add the Frosting runner and register it**

`build/RunnerTests/FrostingRunner.cs`:

```csharp
using Cake.Core;
using Cake.Core.IO;

namespace Build.RunnerTests;

/// <summary>
/// Builds <c>tests/runners/frosting</c> against the requested Cake.Frosting and addin versions, then runs it.
/// </summary>
internal sealed class FrostingRunner : IRunner
{
    public string Name => "frosting";

    public int Run(ICakeContext context, RunnerTestContext test, DirectoryPath workDirectory, DirectoryPath outputDirectory)
    {
        var nugetConfig = NuGetConfig.Write(workDirectory, test.ArtifactsDirectory);
        var binDirectory = workDirectory.Combine("bin");
        var project = test.RunnersDirectory.CombineWithFilePath("frosting/Frosting.csproj");

        var build = RunnerProcess.Run(
            context,
            test,
            "dotnet",
            new ProcessArgumentBuilder()
                .Append("build")
                .AppendQuoted(project.FullPath)
                .Append("--force")
                .AppendSwitch("--configuration", "Release")
                .AppendSwitchQuoted("--output", binDirectory.FullPath)
                .Append($"--property:CakeVersion={test.CakeVersion}")
                .Append($"--property:AddinVersion={test.AddinVersion}")
                .AppendQuoted($"--property:RestoreConfigFile={nugetConfig.FullPath}"),
            workDirectory);
        if (build != 0)
        {
            return build;
        }

        return RunnerProcess.Run(
            context,
            test,
            "dotnet",
            new ProcessArgumentBuilder()
                .AppendQuoted(binDirectory.CombineWithFilePath("Frosting.dll").FullPath)
                .AppendSwitchQuoted("--scenario", "=", test.ScenarioDirectory.FullPath)
                .AppendSwitchQuoted("--output", "=", outputDirectory.FullPath),
            workDirectory);
    }
}
```

In `build/Tasks/RunnerTestsTask.cs`:

```csharp
    private static readonly IRunner[] Runners =
    [
        new ScriptRunner(),
        new SdkRunner(),
        new FrostingRunner(),
    ];
```

- [ ] **Step 3: Run all three runners on both Cake versions**

Run: `dotnet build build/Build.csproj` (expect 0 warnings), then `.\build.ps1 --target RunnerTests`, then `.\build.ps1 --target RunnerTests --cake-version 6.0.0`
Expected: each summary shows `script`, `sdk` and `frosting` as `passed`.

- [ ] **Step 4: Remove the dogfooding project**

```bash
dotnet sln src/Cake.CycloneDX.sln remove src/Cake.CycloneDX.Dogfooding.Build/Cake.CycloneDX.Dogfooding.Build.csproj
git rm -r -q src/Cake.CycloneDX.Dogfooding.Build build/Tasks/DogfoodTask.cs RunDogfooding.ps1
```

In `build/Tasks/AllTask.cs`, delete the line `[IsDependentOn(typeof(DogfoodTask))]`.

- [ ] **Step 5: Verify the build without dogfooding**

Run: `.\build.ps1 --target All`
Expected: Build, Test and Pack succeed. There is no `Dogfood` task in the task summary. `.\build.ps1 --target test` still passes all tests.

- [ ] **Step 6: Verify paths with spaces (Review Focus 5)**

```powershell
git clone --quiet . "$env:TEMP\runner tests space"
Push-Location "$env:TEMP\runner tests space"
.\build.ps1 --target RunnerTests
Pop-Location
Remove-Item -Recurse -Force "$env:TEMP\runner tests space"
```

Expected: all three runners pass. This runs against the committed state, so do it after committing Step 7, or on a branch that has these changes.

- [ ] **Step 7: Commit**

```bash
git add tests/runners/frosting build/RunnerTests/FrostingRunner.cs build/Tasks/RunnerTestsTask.cs build/Tasks/AllTask.cs src/Cake.CycloneDX.sln
git commit -m ":white_check_mark: test: Add the Frosting runner and retire dogfooding" -m "The Frosting runner references the packed addin instead of the project, so it replaces the Cake.CycloneDX.Dogfooding.Build project, the Dogfood task and RunDogfooding.ps1." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Then run Step 6.

---

### Task 5: CI matrix and documentation

**Files:**
- Modify: `.github/workflows/main.yml`
- Modify: `.github/workflows/pr.yml`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: the `RunnerTests` target and `--cake-version` argument (Tasks 2–4).
- Produces: CI jobs named `runner-tests (<os>, <cake>)`.

- [ ] **Step 1: Add the runner-tests job to main.yml**

Append this job under `jobs:` in `.github/workflows/main.yml`, after `build`:

```yaml
  runner-tests:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]
        cake: ["6.0.0", "6.*"]

    steps:
      - name: Checkout
        uses: actions/checkout@v6
        with:
          fetch-depth: 0
      - name: Install .NET SDK
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Install CycloneDX tool
        run: dotnet tool install -g CycloneDX --version 6.1.0
      - name: Run runner tests (Cake ${{ matrix.cake }})
        run: dotnet run --project build/Build.csproj -- --target RunnerTests --cake-version "${{ matrix.cake }}" --verbosity Diagnostic
```

- [ ] **Step 2: Rewrite pr.yml for all OSes, aligned action versions, and runner tests**

Replace the whole `jobs:` section of `.github/workflows/pr.yml` with:

```yaml
jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]

    steps:
      - name: Checkout
        uses: actions/checkout@v6
        with:
          fetch-depth: 0
      - name: Install .NET SDK
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Install .NET 10 SDK (required for CycloneDX tool v6.x)
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.x'
      - name: Install CycloneDX tool
        run: dotnet tool install -g CycloneDX --version 6.1.0
      - name: Build Cake.CycloneDX
        uses: cake-build/cake-action@v3
        with:
          project-path: build/Build.csproj
          target: All
          verbosity: Diagnostic

  runner-tests:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]
        cake: ["6.0.0", "6.*"]

    steps:
      - name: Checkout
        uses: actions/checkout@v6
        with:
          fetch-depth: 0
      - name: Install .NET SDK
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - name: Install CycloneDX tool
        run: dotnet tool install -g CycloneDX --version 6.1.0
      - name: Run runner tests (Cake ${{ matrix.cake }})
        run: dotnet run --project build/Build.csproj -- --target RunnerTests --cake-version "${{ matrix.cake }}" --verbosity Diagnostic
```

- [ ] **Step 3: Validate the workflow YAML**

Run: `python -c "import yaml,sys; [yaml.safe_load(open(f)) for f in sys.argv[1:]]; print('ok')" .github/workflows/main.yml .github/workflows/pr.yml`
Expected: `ok`. If Python/PyYAML is not available, run `npx --yes yaml-lint .github/workflows/main.yml .github/workflows/pr.yml` instead.

- [ ] **Step 4: Document runner tests in AGENTS.md**

In `AGENTS.md`, under "How this repository applies them:", replace the **Testing** bullet with:

```markdown
- **Testing:** unit-test aliases in `src/Cake.CycloneDX.Tests`. Runner tests prove the packed package
  works on the Cake .NET Tool, Cake.Sdk and Cake Frosting:
  `.\build.ps1 --target RunnerTests` (latest Cake 6.x) and
  `.\build.ps1 --target RunnerTests --cake-version 6.0.0` (lowest supported). Run both after changing
  alias signatures, namespaces or package metadata. The scenario lives in `tests/runners/`; keep the
  pipeline in `script/build.cake` and `sdk/cake.cs` identical after the `// --- pipeline ---` line.
  CI runs them on Windows, Linux and macOS for both versions.
```

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/main.yml .github/workflows/pr.yml AGENTS.md
git commit -m ":construction_worker: ci: Run runner tests on all OSes and Cake versions" -m "Adds a runner-tests job (OS x Cake 6.0.0/6.*) to the main and pull request workflows, runs pull request builds on all three OSes, and aligns pr.yml action versions with main.yml." -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 6: Verify in CI**

Push the branch and open a pull request. Ask the user before pushing, since that is outward-facing.
Expected: 3 `build` jobs and 6 `runner-tests` jobs, all green. If a job fails only on Linux or macOS, read its runner summary first; the per-runner failure lines say which step broke.
