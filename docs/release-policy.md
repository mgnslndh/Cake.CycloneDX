# Release Policy

## Purpose

This repository publishes a NuGet package. This policy defines when to create Git tags, GitHub Releases, and NuGet packages, and how to keep them consistent.

## Policy Summary

- Every stable NuGet package publish should have:
  - a matching Git tag
  - a matching GitHub Release
  - generated or curated release notes
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
5. Release notes are generated or reviewed before publishing.

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

GitHub-generated release notes are acceptable by default, provided the repository follows the conventions below.

Generated notes work best when:

- changes are merged through pull requests
- pull requests have clear titles
- pull requests are labeled consistently
- tags are created consistently

Release notes should describe user-visible change. If generated notes are incomplete or noisy, edit them before publishing.

## Required Conventions For Good Generated Notes

To make `gh release create --generate-notes` produce useful output, follow these rules:

1. Use consistent version tags.
2. Merge most changes through pull requests instead of pushing directly to the main branch.
3. Write pull request titles as changelog-ready summaries.
4. Apply labels consistently.
5. Exclude noise through `.github/release.yml` where needed.
6. Use the correct previous tag range when generating notes.

### Tag Rules

- Use one consistent prefix, preferably `v`.
- Do not alternate between formats such as `1.2.3` and `v1.2.3`.
- Do not reuse tags.

### Pull Request Title Rules

Preferred style:

- `Add retry support for package downloads`
- `Fix null handling in metadata parser`
- `Improve diagnostics for restore failures`

Avoid:

- `misc fixes`
- `updates`
- `stuff`

### Label Rules

Recommended labels:

- `breaking-change`
- `feature`
- `enhancement`
- `bug`
- `fix`
- `documentation`
- `chore`
- `ignore-for-release`

These labels can be mapped into release note sections through `.github/release.yml`.

## GitHub Release Configuration

Add a `.github/release.yml` file to group pull requests into useful sections and exclude noise.

Example:

```yaml
changelog:
  exclude:
    labels:
      - ignore-for-release
      - chore
      - documentation
    authors:
      - dependabot
  categories:
    - title: Breaking Changes
      labels:
        - breaking-change
    - title: Features
      labels:
        - feature
        - enhancement
    - title: Fixes
      labels:
        - bug
        - fix
    - title: Other Changes
      labels:
        - "*"
```

## GitHub CLI Usage

### Stable Release

```bash
gh release create v1.2.3 --generate-notes --verify-tag --fail-on-no-commits
```

### Preview Release

```bash
gh release create v1.3.0-preview.1 --generate-notes --prerelease --latest=false
```

### Force Correct Comparison Range

Use this when GitHub would otherwise compare against the wrong previous tag:

```bash
gh release create v1.2.3 --generate-notes --notes-start-tag v1.2.2
```

### Use Tag Annotation Instead Of Generated Notes

If you maintain curated annotated tags:

```bash
gh release create v1.2.3 --notes-from-tag --verify-tag
```

## Recommended Workflow

### Stable

1. Finalize version.
2. Merge release-ready changes.
3. Create tag `vX.Y.Z`.
4. Build and publish package `X.Y.Z`.
5. Create GitHub Release for the tag.
6. Review generated notes before publishing.

### Preview

1. Choose preview version such as `X.Y.Z-preview.N`.
2. Create matching tag.
3. Publish preview package.
4. If externally relevant, create a GitHub prerelease.

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
