# Agent Instructions

## Cake Addin Guidelines

This is a Cake addin and must follow Cake's official guidance. Read these before changing the
public API, package metadata or dependencies:

- [Addin best practices](https://cakebuild.net/docs/extending/addins/best-practices): naming, Cake
  references, target frameworks, package icon, tags, documentation and testing.
- [Creating addins](https://cakebuild.net/docs/extending/addins/creating-addins): alias attributes
  (`CakeMethodAlias`, `CakePropertyAlias`, `CakeNamespaceImport`).
- [Documentation guidelines](https://cakebuild.net/community/contributing/documentation): XML
  documentation for aliases, classes, properties and namespaces.

How this repository applies them:

- **Cake references:** reference `Cake.Core` with `PrivateAssets="all"` so it is never a package
  dependency, and keep it at the lowest compatible version (currently 6.0.0). Only raise it when a
  newer Cake API is required, and say why in the commit message. Don't reference `Cake.Common`
  unless an alias needs it. When Cake releases a new major version, update the reference and
  the target frameworks to match the best-practices page.
- **Target frameworks:** match the frameworks the best-practices page lists (currently `net8.0`,
  `net9.0` and `net10.0`) in the addin, test and helper projects.
- **New aliases:** put them in a `static` class ending in `Aliases`, marked
  `[CakeAliasCategory("CycloneDX")]`, as `ICakeContext` extension methods marked `[CakeMethodAlias]`.
  If an alias takes a type from a namespace other than its class's, add
  `[CakeNamespaceImport("<namespace>")]` **on the method**. Cake.Sdk ignores class- and
  assembly-level imports. `AliasNamespaceImportTests` enforces this.
- **New namespaces:** add a `NamespaceDoc` class for them in `src/Cake.CycloneDX/Namespaces.cs`.
- **Package metadata:** keep the `cake-addin` tag and the embedded icon in `src/Directory.Build.props`.
- **Testing:** test aliases in the unit tests, and verify behavior across Cake runners and
  operating systems as described in the best practices.

## Style and Analyzers

This project enforces **StyleCop** rules as **errors** during the full `.\build.ps1` build
(for all target frameworks: `net8.0`, `net9.0` and `net10.0`). `dotnet test` on the test project alone treats
these as warnings, so a green `dotnet test` run does **not** guarantee a clean build.

Always run `.\build.ps1 --target test` to validate changes, not just `dotnet test`.

### Rules that have previously caused build failures

| Rule | Description | Fix |
|------|-------------|-----|
| **SA1503** | Braces must not be omitted | Always write braces for `if`, `else`, `foreach`, `while`, `for`, and `using` bodies — even single-line ones. No braceless control-flow statements. |
| **SA1600** / **CS1591** | Public elements must be documented | Every public type and member in `src/Cake.CycloneDX/` needs XML docs. Aliases need `<summary>`, `<param>`, `<returns>`, `<exception>` and a script-style `<example><code>`, per the [Cake documentation guidelines](https://cakebuild.net/community/contributing/documentation). Use `<content>` on additional parts of a partial class (SA1601). |

### General guidance

- After editing any `src/Cake.CycloneDX/` source file, run `.\build.ps1 --target test` to catch analyzer errors that `dotnet test` alone will miss.
- The ruleset is in `src/CodeAnalysis.ruleset`; StyleCop settings are in `src/StyleCop.json`.
- Documentation rules only apply to projects that set `GenerateDocumentationFile` (the addin); see `src/Directory.Build.targets`.
