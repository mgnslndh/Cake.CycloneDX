# Runner Tests Design

## Goal

Prove that the published Cake.CycloneDX package works on every official Cake runner, on the lowest and
the latest supported Cake version, on Windows, Linux and macOS. This follows the Cake addin best
practices §5.1–5.3 (<https://cakebuild.net/docs/extending/addins/best-practices>).

Success means that, for each OS and each tested Cake version:

- the **Cake .NET Tool** (`build.cake` with `#addin`), **Cake.Sdk** (file-based `cake.cs` with `#:sdk`
  and `#:package`) and **Cake Frosting** (`PackageReference`) runners all load the **packed `.nupkg`**
  from a local feed and run the same full SBOM pipeline against the real CycloneDX tools;
- the resulting SBOMs contain the expected content;
- the three runners produce equivalent SBOMs.

## Decisions

| Topic | Decision |
|-------|----------|
| What is tested | The packed `.nupkg` from `artifacts/`, never a project reference. |
| Runners | Cake .NET Tool, Cake.Sdk (file-based only), Cake Frosting. |
| Cake versions | Lowest supported (`6.0.0`) and latest 6.x (`6.*`, floating). |
| Depth | Full pipeline with the real CycloneDX .NET tool and CycloneDX CLI. |
| Comparison | The three runners are compared with each other within a job, plus hand-written asserts on key content. OSes and Cake versions are not compared with each other. |
| Orchestration | A Frosting task in `build/` (`RunnerTests`); CI only supplies a matrix. |
| OSes | Windows, Linux and macOS, for both pushes to `main` and pull requests. |

## Layout

```
build/
  Tasks/RunnerTestsTask.cs        orchestrates the runners, asserts, reports
  Tools/                          CycloneDxCliDownloader and helpers, moved from the dogfooding project
  RunnerTests/                    small classes: CakeVersionResolver, SbomNormalizer, SbomAssertions
tests/runners/                    outside src/, so src/Directory.*.props (including central package
                                  management) do not apply; there are no repository-root equivalents
  nuget.config                    local feed (artifacts/) + nuget.org
  scenario/                       fixture: App and Lib class libraries (App references Lib) with a few package references
  script/build.cake               Cake .NET Tool runner
  sdk/cake.cs                     Cake.Sdk runner (file-based)
  frosting/Frosting.csproj        Cake Frosting runner
```

`src/Cake.CycloneDX.Dogfooding.Build/`, `build/Tasks/DogfoodTask.cs` and `RunDogfooding.ps1` are
removed. The Frosting runner replaces them and tests the package instead of a project reference.

## Orchestration (`RunnerTestsTask`)

Depends on `Pack`. Takes `--cake-version` (default `6.*`).

1. **Resolve versions.** Read the addin version from the packed `.nupkg` in `artifacts/`. Resolve
   `--cake-version` to one concrete version via the NuGet flat-container API (`6.*` → the highest
   non-prerelease 6.x). Log both. Every runner in the run uses this one Cake version.
2. **Check prerequisites; fail fast with an actionable message if missing.** The CycloneDX .NET tool
   must be installed (`dotnet tool install -g CycloneDX`). Download the CycloneDX CLI into `tools/` with
   the moved downloader.
3. **Run each runner**, each writing to `artifacts/runner-tests/<cake-version>/<runner>/`:
   - **Script:** install `Cake.Tool` at the Cake version into `tools/cake-<version>` and run
     `dotnet-cake tests/runners/script/build.cake` with the addin version and the output folder as
     arguments. `#addin nuget:?package=Cake.CycloneDX&version=<addin version>` resolves from the
     `nuget.config` feed.
   - **Cake.Sdk:** run `dotnet cake.cs` with `#:sdk Cake.Sdk@<cake version>` and
     `#:package Cake.CycloneDX@<addin version>`. The exact way these versions get into the file
     (a generated copy of `cake.cs`, or MSBuild properties) is decided by a check at the start of
     the implementation plan.
   - **Frosting:** `dotnet run --project tests/runners/frosting` with `-p:CakeVersion=<cake version>`
     and `-p:AddinVersion=<addin version>`, which set the `Cake.Frosting` and `Cake.CycloneDX`
     `PackageReference` versions.
4. **Assert** (see below).
5. **Report:** a summary table of runner × result × reason. The task fails if any runner or assertion
   failed. A failing runner does not stop the others, so one job shows every broken runner.

## Scenario (identical in all three runners)

1. `CdxDotNet(FilePath)` for `App` and `Lib`, into the runner's output folder.
2. `CdxCliMerge` with `Group`, `Name` and `Version` set.
3. `CdxDeduplicate(input, output)`.
4. `CdxRefine(input, output)` with orphan adoption, one exclusion, orphan removal, one group rule and
   one type rule.
5. `CdxCliValidate(files)` with `FailOnErrors = true`.

The script and `cake.cs` contain **no `using` directives** for Cake.CycloneDX namespaces. If settings
types in sub-namespaces (for example `Cake.CycloneDX.Tools.CdxCli.Merge`) cannot be resolved, that is
a bug in the addin, fixed there (for example with `[CakeNamespaceImport]`), not worked around in the
scripts.

## Assertions (in C#, in `RunnerTestsTask`)

Per runner:

- the runner exited with code 0 and the final SBOM exists;
- the SBOM contains the `App` and `Lib` components and the expected packages;
- the excluded component is absent;
- the group rule and type rule are applied;
- the metadata component has the merge `Group`, `Name` and `Version`.

Across the three runners in the same job:

- the SBOMs are equivalent after normalization. Normalization removes the serial number, timestamps
  and tool metadata, and sorts components and dependencies. A mismatch fails the run, naming the two
  runners and the first element that differs.

## CI

- New `runner-tests` job in `main.yml` and `pr.yml`: `os: [windows-latest, ubuntu-latest, macos-latest]`
  × `cake: ["6.0.0", "6.*"]`, `fail-fast: false`, running
  `dotnet run --project build/Build.csproj -- --target RunnerTests --cake-version ${{ matrix.cake }}`
  after the existing SDK and CycloneDX tool setup steps.
- The existing `build` job keeps `--target All` (without `Dogfood`); in `pr.yml` it runs on all three
  OSes.
- Align `pr.yml` action versions with `main.yml` (`checkout@v6`, `setup-dotnet@v5`).

## Local use

- `.\build.ps1 --target RunnerTests` tests the latest 6.x.
- `.\build.ps1 --target RunnerTests --cake-version 6.0.0` tests the lowest supported version.
- `AGENTS.md` points to these commands.

## Verifying the harness

Each check must be seen failing once before it is trusted: break an expected component, make one
runner's output differ from the others, and request a Cake version that does not exist. There is no
separate test project for `build/`.

## Out of scope

- The project-based Cake.Sdk variant (it uses the same source generator as the file-based one).
- Cake versions between 6.0.0 and the latest 6.x, and Cake 7.x.
- JSON-format SBOMs.
- Comparing outputs across OSes or Cake versions (for example with a checked-in expected SBOM).
