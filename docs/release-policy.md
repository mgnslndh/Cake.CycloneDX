# Release Policy

## Purpose

This repository publishes a NuGet package. This policy defines when to create Git tags, GitHub Releases, and NuGet packages, and how to keep them consistent.

## Policy Summary

- Every stable NuGet package publish should have:
  - a matching Git tag
  - a matching GitHub Release
  - release notes from `CHANGELOG.md`
- Preview NuGet packages may have a GitHub prerelease when the preview is intended for external users and needs release notes or visibility.
- Nightly, CI, internal, or temporary validation packages should normally not have a GitHub Release.

## Versioning

Use Semantic Versioning.

Examples:

- Stable: `1.2.3`
- Preview: `1.3.0-preview.1`
- Release candidate: `1.3.0-rc.1`
- Alpha (untagged build): `1.3.0-alpha.0.4`

Tag format:

- Stable: `v1.2.3`
- Preview: `v1.3.0-preview.1`
- RC: `v1.3.0-rc.1`

Alpha versions are **not tagged**. They are produced automatically by MinVer from the commit height since the last tag. Do not create tags of the form `v1.3.0-alpha.0.4`.

The Git tag, GitHub Release, and NuGet package version should represent the same shipped version. This applies to stable and preview releases only — alpha builds are not shipped releases.

## Stable Releases

Create a GitHub Release for every stable package published to NuGet.

This is the default release path because stable versions are human-facing milestones and should have:

- a discoverable release page
- release notes for consumers
- a clear changelog history
- a stable tag tied to the shipped code

Minimum requirements for a stable release:

1. The version is finalized.
2. The package has been built from the tagged commit.
3. The tag matches the package version.
4. A GitHub Release is published for the tag.
5. `CHANGELOG.md` has a reviewed `[X.Y.Z]` section, which becomes the release notes.

## Preview Releases

Preview packages do not always require a GitHub Release.

Create a GitHub prerelease when:

- the preview is announced to users outside the core team
- consumers need notes about changes, migration steps, or known limitations
- the preview is part of a planned validation cycle

Do not create a GitHub Release when:

- the package is only for CI validation
- the package is internal or temporary
- the build is a disposable test artifact

When a preview release is created:

- use a prerelease package version such as `1.4.0-preview.2`
- create a matching tag such as `v1.4.0-preview.2`
- publish a GitHub Release marked as prerelease
- do not mark it as latest

## Alpha, Nightly, and CI Packages

Untagged commits on `main` automatically produce alpha versions such as `1.3.0-alpha.0.4` via MinVer. These are not published to NuGet.org and do not get a GitHub Release.

These packages are not part of the public release history.

## Source of Truth

The shipped source for a released package must be recoverable from Git.

That means:

- every public stable release must have a tag
- the package should be built from the tagged commit or a reproducible equivalent
- the release page should reference the same version as the package

## Release Notes Policy

`CHANGELOG.md` is the source of truth for release notes. It follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/):

- Add user-visible changes to the `## [Unreleased]` section as they are merged, under `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed` or `Security`.
- Leave out changes users can't see, such as CI, tests and internal build work.
- Mark breaking changes with **Breaking:** and say how to update.
- When releasing, rename `[Unreleased]` to `[X.Y.Z] - YYYY-MM-DD`, add a new empty `[Unreleased]` section and update the compare links at the bottom.
- Use that version's section as the GitHub Release notes.

The release checks the CHANGELOG before it builds anything, and stops when it isn't ready for the tag:

- A stable tag `vX.Y.Z` needs a `[X.Y.Z]` section that is the newest version in the file and isn't empty, and `[Unreleased]` must be empty. Entries left in `[Unreleased]` are part of the tagged commit but would be missing from the notes.
- A prerelease tag `vX.Y.Z-preview.N` with its own `[X.Y.Z-preview.N]` section follows the same rules.
- A prerelease tag without its own section uses the `[Unreleased]` section, which must not be empty, since a preview ships the changes gathered there so far. The tag must be newer than the newest version in the file, so a preview of an already released version is rejected.
- A stable tag also needs the README's install snippets (`#addin …&version=X.Y.Z` and `#:package Cake.CycloneDX@X.Y.Z`) to name `X.Y.Z`, because the README ships in the package as its nuget.org page. Prerelease tags skip this check, so during a preview the README keeps pointing at the latest stable version.

Release notes should describe user-visible change, not list commits or pull requests.

### Tag Rules

- Use the prefix `v` for every release tag.
- Do not alternate between formats such as `1.2.3` and `v1.2.3`.
- Do not reuse tags.

## Recommended Workflow

### Stable

1. Finalize version.
2. Merge release-ready changes.
3. In `CHANGELOG.md`, rename `[Unreleased]` to `[X.Y.Z] - YYYY-MM-DD`, add a new empty `[Unreleased]` section and update the compare links. In `README.md`, update the install snippets to `X.Y.Z` and the supported Cake versions if they changed. Merge that change.
4. Run `./release.ps1 X.Y.Z` (or `-Bump`/`-Promote`) on that commit. It creates and pushes the tag; the Release workflow builds, publishes the package and creates the GitHub Release with the CHANGELOG section as notes.

### Preview

1. Choose preview version such as `X.Y.Z-preview.N`.
2. Check that `[Unreleased]` in `CHANGELOG.md` describes the preview (or add a `[X.Y.Z-preview.N]` section).
3. Run `./release.ps1 -Bump Minor -Prerelease preview` (or `-Prerelease preview` for the next preview in a series). It creates and pushes the tag; the Release workflow publishes the package and a GitHub prerelease.

## Decision Rules

Use this quick rule set:

- Stable package published publicly: create GitHub Release.
- Preview package published publicly and meant for evaluation: usually create GitHub prerelease.
- Preview package for internal testing only: no GitHub Release.
- Nightly or CI package: no GitHub Release.

## Exceptions

It is acceptable to skip a GitHub Release for a stable package only in unusual cases, such as:

- emergency operational republish with no code change
- private package feed with no human release audience
- migration period while formal release process is being introduced

If an exception is used, document the reason in the repository or deployment log.

## Ownership

The maintainer publishing the package is responsible for ensuring:

- version correctness
- tag correctness
- release note quality
- prerelease versus stable classification

## Default Rule For This Repository

Unless explicitly decided otherwise:

- every public stable NuGet package gets a GitHub Release
- every externally shared preview package gets a GitHub prerelease
- internal, CI, and temporary packages do not get GitHub Releases

## Automation In This Repository

- Pull requests and pushes to `main` run `build --target All` (build, test, pack) and the runner tests on Cake 6.0.0 and 6.* on Windows, Linux and macOS.
- The tag push is the review point: there is no manual step between pushing a `v*` tag and the package appearing on nuget.org with a GitHub (pre)release, so review the changes and the notes before tagging. A preview that should not get a GitHub Release (internal or CI-only) must not be tagged.
- Create and push the tag with `./release.ps1` (PowerShell 7.2+, `git`, `gh` and the .NET SDK), e.g. `./release.ps1 -Bump Minor`, `./release.ps1 -Bump Minor -Prerelease preview` or `./release.ps1 -Promote`. Run it without arguments to see the latest release and suggested next versions; it changes nothing. With a version it checks the branch, working tree, CI status, tag uniqueness and the CHANGELOG gate (see Release Notes Policy), shows a summary and asks before tagging, then follows the Release workflow. Use `-WhatIf` for a dry run. Its version logic in `build/Release.psm1` is tested with Pester 5+: `Invoke-Pester ./tests/Release.Tests.ps1`.
- Pushing a tag `vX.Y.Z` (or `vX.Y.Z-preview.N`) runs `.github/workflows/release.yml`: `Release-Notes` checks that `CHANGELOG.md` has notes for the tag (and, for a stable tag, that the README installs that version), then the same `All` build and runner tests run, all in a job without write permissions, then — in a separate `publish` job that only receives the built package — the `Release` target, which runs three steps in this order:
  1. `Draft-Release` creates the GitHub Release as a **draft** (CHANGELOG notes, package attached; invisible to users). It refuses if there is no package whose version equals the tag.
  2. `Publish` pushes `artifacts/Cake.CycloneDX.X.Y.Z.nupkg` to nuget.org — the only irreversible step.
  3. `Release` publishes the draft (stable tags become latest; tags containing `-` become prereleases that are not latest).
- If the release job fails, re-run it ("Re-run failed jobs"): every step is safe to repeat. An existing draft is reused and gets its package replaced, NuGet skips a version that is already there, and an already published Release is left alone. If the failure happened before `Publish`, you can instead delete the draft and the tag (`gh release delete vX.Y.Z --cleanup-tag`) and release again.

## Publishing Setup (one-time, repository owner)

1. Create the GitHub environment `Production` in `mgnslndh/Cake.CycloneDX`.
2. Add the secret `NUGET_USER` (the nuget.org account name) to that environment.
3. On nuget.org, add a Trusted Publishing policy for `Cake.CycloneDX`: owner `mgnslndh`, repository `Cake.CycloneDX`, workflow file `release.yml`, environment `Production`.
