# Agent Instructions

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
- The addin references the lowest compatible Cake version (`Cake.Core` 6.0.0), per the Cake addin best practices. Don't raise it unless a newer Cake API is required.
