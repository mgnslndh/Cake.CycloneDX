# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `CdxRefine` can exclude components by name, purl or bom-ref with `WithExcludeByName`,
  `WithExcludeByPurl` and `WithExcludeByBomRef`.
- `CdxRefine` can remove orphaned top-level components (components not reachable from the
  metadata component) with `WithRemoveOrphanedComponents`. It logs nested components that are
  unreachable but kept.
- `CdxRefine` can adopt orphaned components by linking them to a parent component with
  `WithAdoptionByName`, `WithAdoptionByPurl`, `WithAdoptionByBomRef` and
  `WithAdoptOrphanedComponents`.
- The package now ships XML documentation, so IntelliSense in build scripts shows docs for all
  aliases and settings.

### Changed

- The addin now supports Cake 6.0.0 and later. It previously required Cake 6.1.0.
- `CdxRefine` resolves dependency reachability much faster on large SBOMs.
- **Breaking:** the second parameter of `WithTypeByPurl` was renamed from `bomRefPattern` to
  `purlPattern`. Update calls that pass it as a named argument.
- The package README now covers installation on each Cake runner, the CycloneDX tools the aliases
  need and how they are found, and every alias, starting with a complete SBOM pipeline.

### Fixed

- `CdxCliMergeSettings.Group` is now passed to the CycloneDX CLI as `--group`.
- Build scripts can use `CdxCliMergeSettings` and `CdxCliValidateSettings` without `using`
  directives on both the Cake .NET Tool and Cake.Sdk.
- The CycloneDX CLI is now found when it is named `cyclonedx.exe`, the usual name on Windows,
  without setting `ToolPath` ([#7](https://github.com/mgnslndh/Cake.CycloneDX/issues/7)).
- `CdxRefine` group rules now apply when the SBOM has no `<metadata>` or no `<components>`
  element ([#6](https://github.com/mgnslndh/Cake.CycloneDX/issues/6)).
- Corrected the XML doc examples for `CdxDeduplicate` and `CdxRefine`, which used aliases and
  namespaces that aren't available in a plain Cake script.

## [0.0.5] - 2026-04-10

First public release.

### Added

- `CdxDotNet` alias to run the CycloneDX .NET tool (v6.x).
- `CdxCliMerge` and `CdxCliValidate` aliases to run the CycloneDX CLI.
- `CdxDeduplicate` alias to remove duplicate components from an SBOM and update dependency
  references.
- `CdxRefine` alias to set component groups and types by name, purl or bom-ref.
- The CycloneDX tools are resolved on Windows, Linux and macOS.
- Targets `net8.0`, `net9.0` and `net10.0`, for Cake 6.1.0 and later.

[Unreleased]: https://github.com/mgnslndh/Cake.CycloneDX/compare/v0.0.5...HEAD
[0.0.5]: https://github.com/mgnslndh/Cake.CycloneDX/releases/tag/v0.0.5
