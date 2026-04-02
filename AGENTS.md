# Agent Instructions

## Style and Analyzers

This project enforces **StyleCop** rules as **errors** during the full `.\build.ps1` build
(both `net8.0` and `net9.0` target frameworks). `dotnet test` on the test project alone treats
these as warnings, so a green `dotnet test` run does **not** guarantee a clean build.

Always run `.\build.ps1 --target test` to validate changes, not just `dotnet test`.

### Rules that have previously caused build failures

| Rule | Description | Fix |
|------|-------------|-----|
| **SA1503** | Braces must not be omitted | Always write braces for `if`, `else`, `foreach`, `while`, `for`, and `using` bodies — even single-line ones. No braceless control-flow statements. |

### General guidance

- After editing any `src/Cake.CycloneDX/` source file, run `.\build.ps1 --target test` to catch analyzer errors that `dotnet test` alone will miss.
- The ruleset is in `src/CodeAnalysis.ruleset`; StyleCop settings are in `src/StyleCop.json`.
