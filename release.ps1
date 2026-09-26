<#
.SYNOPSIS
Tags and pushes a Cake.CycloneDX release. The Release workflow then builds, tests, publishes to NuGet and creates the
GitHub Release.

.DESCRIPTION
Without arguments, release.ps1 only shows the latest release, the state of your checkout and suggested next versions.
It changes nothing.

With a version, -Bump, -Prerelease or -Promote it:
  1. checks that you are on an up-to-date, clean main whose CI run is green, that the tag is new and higher
     than the latest release, and that CHANGELOG.md is ready for it (the same Release-Notes gate the Release
     workflow runs first);
  2. shows a summary and asks for confirmation (use -WhatIf for a dry run, -Confirm:$false to skip the prompt);
  3. creates the annotated tag v<version> and pushes it;
  4. follows the Release workflow run and prints the NuGet and GitHub Release links (skip with -NoWait).

Release tags look like v1.2.0 or, for prereleases, v1.2.0-preview.1. See docs/release-policy.md.

.PARAMETER Version
The exact version to release, with or without the leading v: 1.2.0, v1.2.0, 1.3.0-rc.1.

.PARAMETER Bump
Release the next Major, Minor or Patch version after the latest release. Combine with -Prerelease to start a
prerelease series of that version.

.PARAMETER Prerelease
The prerelease label, e.g. preview or rc. On its own it releases the next prerelease of the current series
(v1.2.0-preview.1 -> v1.2.0-preview.2, or v1.2.0-preview.3 -> v1.2.0-rc.1).

.PARAMETER Promote
Release the latest prerelease as stable (v1.2.0-rc.2 -> v1.2.0).

.PARAMETER Remote
The git remote to push the tag to. Default: origin.

.PARAMETER NoWait
Do not follow the Release workflow run after pushing the tag.

.PARAMETER SkipCiCheck
Do not require a successful CI run on the commit being released.

.EXAMPLE
./release.ps1
Shows the latest release and suggested next versions. Changes nothing.

.EXAMPLE
./release.ps1 -Bump Minor
Releases v1.3.0 after v1.2.4.

.EXAMPLE
./release.ps1 -Bump Minor -Prerelease preview
Releases v1.3.0-preview.1 after v1.2.4.

.EXAMPLE
./release.ps1 -Prerelease preview
Releases v1.3.0-preview.2 after v1.3.0-preview.1.

.EXAMPLE
./release.ps1 -Promote
Releases v1.3.0 after v1.3.0-preview.2.

.EXAMPLE
./release.ps1 1.3.0-rc.1 -WhatIf
Runs every check and shows what would happen, without tagging or pushing.
#>
#Requires -Version 7.2
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High', DefaultParameterSetName = 'Status')]
param(
    [Parameter(Mandatory, Position = 0, ParameterSetName = 'Version')]
    [ValidatePattern('^v?\d+\.\d+\.\d+(-[A-Za-z]+\.\d+)?$', ErrorMessage = "'{0}' is not a release version like 1.2.0 or 1.2.0-preview.1.")]
    [string] $Version,

    [Parameter(Mandatory, ParameterSetName = 'Bump')]
    [ValidateSet('Major', 'Minor', 'Patch')]
    [string] $Bump,

    [Parameter(ParameterSetName = 'Bump')]
    [Parameter(Mandatory, ParameterSetName = 'Prerelease')]
    [ValidatePattern('^[A-Za-z]+$', ErrorMessage = "'{0}' is not a prerelease label; use letters only, e.g. preview or rc.")]
    [ArgumentCompleter({ param($command, $parameter, $word) 'preview', 'rc', 'beta', 'alpha' | Where-Object { $_ -like "$word*" } })]
    [string] $Prerelease,

    [Parameter(Mandatory, ParameterSetName = 'Promote')]
    [switch] $Promote,

    [string] $Remote = 'origin',

    [switch] $NoWait,

    [switch] $SkipCiCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'build' 'Release.psm1') -Force

$Branch = 'main'
$CiWorkflow = 'main.yml'
$ReleaseWorkflow = 'release.yml'
$PackageId = 'Cake.CycloneDX'

function Invoke-Tool {
    # Runs a native command, returns its standard output lines and throws with its error output on a non-zero exit code.
    param([Parameter(Mandatory)] [string] $Name, [Parameter(Mandatory)] [string[]] $Arguments)

    $output = & $Name @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $stdout = @($output | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
    if ($exitCode -ne 0) {
        $stderr = @($output | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { "$_" })
        throw "'$Name $($Arguments -join ' ')' failed (exit code $exitCode):`n$(($stderr + $stdout) -join "`n")"
    }
    return $stdout
}

function Test-Tool {
    # Runs a native command only for its exit code.
    param([Parameter(Mandatory)] [string] $Name, [Parameter(Mandatory)] [string[]] $Arguments)

    & $Name @Arguments *> $null
    return $LASTEXITCODE -eq 0
}

function Write-Line([string] $Label, [string] $Value) {
    Write-Host ("  {0}{1,-16}{2} {3}" -f $PSStyle.Foreground.BrightBlack, $Label, $PSStyle.Reset, $Value)
}

function Format-Check([bool] $Ok, [string] $Text) {
    $symbol = $Ok ? "$($PSStyle.Foreground.Green)✔" : "$($PSStyle.Foreground.Red)✖"
    return "$symbol$($PSStyle.Reset) $Text"
}

function Get-CiStatus([string] $Commit) {
    $json = Invoke-Tool gh @('run', 'list', '--workflow', $CiWorkflow, '--commit', $Commit, '--limit', '1', '--json', 'status,conclusion,url')
    $run = @($json -join "`n" | ConvertFrom-Json) | Select-Object -First 1
    if ($null -eq $run) { return [pscustomobject]@{ Ok = $false; Text = "no $CiWorkflow run for this commit"; Url = $null } }
    if ($run.status -ne 'completed') { return [pscustomobject]@{ Ok = $false; Text = "CI is still $($run.status)"; Url = $run.url } }
    return [pscustomobject]@{ Ok = $run.conclusion -eq 'success'; Text = $run.conclusion; Url = $run.url }
}

function Get-ChangelogStatus([string] $Tag) {
    # Runs the CHANGELOG.md release gate (the Release-Notes build target) for the tag, as the Release workflow will.
    Write-Progress -Activity 'release.ps1' -Status "Checking CHANGELOG.md for $Tag"
    $previous = $env:GITHUB_REF_NAME
    try {
        $env:GITHUB_REF_NAME = $Tag
        $output = & dotnet run --project build/Build.csproj -- --target Release-Notes 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $env:GITHUB_REF_NAME = $previous
        Write-Progress -Activity 'release.ps1' -Completed
    }

    if ($exitCode -eq 0) { return [pscustomobject]@{ Ok = $true; Text = "release notes for $Tag are ready" } }
    $message = @($output | ForEach-Object { "$_" } | Where-Object { $_ -match '^Error: ' }) | Select-Object -First 1
    $text = $null -ne $message ? $message.Substring('Error: '.Length) : "the Release-Notes target failed (exit code $exitCode)"
    return [pscustomobject]@{ Ok = $false; Text = $text }
}

function Get-RepositoryState {
    Write-Progress -Activity 'release.ps1' -Status "Fetching $Remote/$Branch and tags"
    Invoke-Tool git @('fetch', '--quiet', '--tags', $Remote, $Branch) | Out-Null

    Write-Progress -Activity 'release.ps1' -Status 'Reading repository state'
    $head = @(Invoke-Tool git @('rev-parse', 'HEAD'))[0]
    $latest = Get-LatestReleaseVersion -Tags @(Invoke-Tool git @('tag', '--list', 'v*'))
    $range = $null -eq $latest ? 'HEAD' : "$($latest.Tag)..HEAD"

    $ci = if ($SkipCiCheck) { [pscustomobject]@{ Ok = $true; Text = 'not checked (-SkipCiCheck)'; Url = $null } }
          else { Write-Progress -Activity 'release.ps1' -Status 'Checking CI'; Get-CiStatus $head }
    Write-Progress -Activity 'release.ps1' -Completed

    [pscustomobject]@{
        Head          = $head
        Short         = @(Invoke-Tool git @('rev-parse', '--short', 'HEAD'))[0]
        Subject       = @(Invoke-Tool git @('log', '-1', '--format=%s'))[0]
        Branch        = (Invoke-Tool git @('branch', '--show-current')) | Select-Object -First 1
        RemoteHead    = @(Invoke-Tool git @('rev-parse', "$Remote/$Branch"))[0]
        Dirty         = @(Invoke-Tool git @('status', '--porcelain', '--untracked-files=no')).Count -gt 0
        Latest        = $latest
        CommitsSince  = [int]@(Invoke-Tool git @('rev-list', '--count', $range))[0]
        Ci            = $ci
        Changelog     = $null
    }
}

function Get-Problems($State, $Next) {
    $problems = [System.Collections.Generic.List[string]]::new()
    if (-not (Test-Tool gh @('auth', 'status'))) { $problems.Add("gh is not logged in; run 'gh auth login'.") }
    if ($State.Branch -ne $Branch) { $problems.Add("You are on '$($State.Branch)', not '$Branch'.") }
    if ($State.Dirty) { $problems.Add('The working tree has uncommitted changes to tracked files.') }
    if ($State.Head -ne $State.RemoteHead) {
        $problems.Add("HEAD ($($State.Short)) is not $Remote/$Branch; pull or push first so the release commit is the one on $Remote.")
    }
    if ($null -ne $State.Latest -and $State.CommitsSince -eq 0) { $problems.Add("There are no commits since $($State.Latest.Tag).") }
    if (-not $State.Ci.Ok) {
        $problems.Add("CI on this commit is not green ($($State.Ci.Text)). $($State.Ci.Url) Use -SkipCiCheck to release anyway.")
    }
    if ($null -ne $State.Changelog -and -not $State.Changelog.Ok) { $problems.Add($State.Changelog.Text) }
    if ($null -ne $Next) {
        if (Test-Tool git @('rev-parse', '--quiet', '--verify', "refs/tags/$($Next.Tag)")) { $problems.Add("Tag $($Next.Tag) already exists locally.") }
        if (@(Invoke-Tool git @('ls-remote', '--tags', $Remote, "refs/tags/$($Next.Tag)")).Count -gt 0) {
            $problems.Add("Tag $($Next.Tag) already exists on $Remote.")
        }
    }
    return $problems
}

function Show-State($State, $Next) {
    Write-Host ''
    $latestText = $null -eq $State.Latest ? 'none yet' : "$($State.Latest.Tag) ($($State.CommitsSince) commits since)"
    Write-Line 'Latest release' $latestText
    if ($null -ne $Next) { Write-Line 'Next release' "$($PSStyle.Bold)$($Next.Tag)$($PSStyle.Reset)" }
    Write-Line 'Commit' "$($State.Short)  $($State.Subject)"
    Write-Line 'Branch' (Format-Check ($State.Branch -eq $Branch -and $State.Head -eq $State.RemoteHead -and -not $State.Dirty) "$($State.Branch), $(if ($State.Head -eq $State.RemoteHead) { "up to date with $Remote/$Branch" } else { "differs from $Remote/$Branch" })$(if ($State.Dirty) { ', uncommitted changes' })")
    Write-Line 'CI on commit' (Format-Check $State.Ci.Ok $State.Ci.Text)
    if ($null -ne $State.Changelog) { Write-Line 'CHANGELOG.md' (Format-Check $State.Changelog.Ok $State.Changelog.Text) }
    Write-Host ''
}

function Show-Suggestions($Latest) {
    $candidates = if ($null -eq $Latest) {
        @([ordered]@{ Bump = 'Minor' }, [ordered]@{ Bump = 'Minor'; Prerelease = 'preview' }, [ordered]@{ Version = '1.0.0' })
    }
    elseif ($Latest.IsPrerelease()) {
        @([ordered]@{ Promote = $true }, [ordered]@{ Prerelease = $Latest.Label }, [ordered]@{ Bump = 'Minor' })
    }
    else {
        @([ordered]@{ Bump = 'Patch' }, [ordered]@{ Bump = 'Minor' }, [ordered]@{ Bump = 'Minor'; Prerelease = 'preview' })
    }

    Write-Host 'Suggested:'
    foreach ($candidate in $candidates) {
        $next = Get-NextReleaseVersion -Latest $Latest @candidate
        $arguments = foreach ($key in $candidate.Keys) {
            if ($key -eq 'Version') { $candidate[$key] } elseif ($key -eq 'Promote') { '-Promote' } else { "-$key $($candidate[$key])" }
        }
        Write-Host ("  {0,-45} {1}-> {2}{3}" -f "./release.ps1 $($arguments -join ' ')", $PSStyle.Foreground.BrightBlack, $PSStyle.Reset, $next.Tag)
    }
    Write-Host "Run 'Get-Help ./release.ps1 -Examples' for more."
}

function Wait-ReleaseRun($Next) {
    $repoUrl = try { @(Invoke-Tool gh @('repo', 'view', '--json', 'url', '--jq', '.url'))[0] } catch { $null }
    $actionsUrl = $null -eq $repoUrl ? "the $ReleaseWorkflow workflow on GitHub Actions" : "$repoUrl/actions/workflows/$ReleaseWorkflow"
    $nugetUrl = "https://www.nuget.org/packages/$PackageId/$($Next.Tag.Substring(1))"
    $undo = "git push --delete $Remote $($Next.Tag); git tag --delete $($Next.Tag)"

    if ($NoWait) {
        Write-Host "Follow the release: $actionsUrl"
        return
    }

    $run = $null
    $deadline = (Get-Date).AddSeconds(90)
    while ($null -eq $run -and (Get-Date) -lt $deadline) {
        Write-Progress -Activity 'release.ps1' -Status "Waiting for the $ReleaseWorkflow run of $($Next.Tag) to start"
        Start-Sleep -Seconds 3
        $json = Invoke-Tool gh @('run', 'list', '--workflow', $ReleaseWorkflow, '--branch', $Next.Tag, '--limit', '1', '--json', 'databaseId,url')
        $run = @($json -join "`n" | ConvertFrom-Json) | Select-Object -First 1
    }
    Write-Progress -Activity 'release.ps1' -Completed

    if ($null -eq $run) {
        Write-Warning "No $ReleaseWorkflow run for $($Next.Tag) appeared within 90 seconds. Check $actionsUrl"
        return
    }

    Write-Host "Following $($run.url)"
    & gh run watch $run.databaseId --exit-status --interval 10
    if ($LASTEXITCODE -eq 0) {
        Write-Host ''
        Write-Host "$($PSStyle.Foreground.Green)Released $($Next.Tag)$($PSStyle.Reset)"
        Write-Line 'NuGet' "$nugetUrl (indexing can take a few minutes)"
        Write-Line 'GitHub' "$repoUrl/releases/tag/$($Next.Tag)"
        return
    }

    Write-Host ''
    Write-Host "$($PSStyle.Foreground.Red)The Release workflow failed: $($run.url)$($PSStyle.Reset)"
    Write-Host "If nothing was published (check $nugetUrl), remove the tag and try again:"
    Write-Host "  $undo"
    exit 1
}

$location = Get-Location
try {
    Set-Location (@(Invoke-Tool git @('rev-parse', '--show-toplevel'))[0])
    foreach ($tool in 'git', 'gh') {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "$tool is not installed or not on PATH." }
    }

    $state = Get-RepositoryState

    if ($PSCmdlet.ParameterSetName -eq 'Status') {
        Show-State $state $null
        $problems = @(Get-Problems $state $null)
        if ($problems.Count -gt 0) {
            Write-Host "$($PSStyle.Foreground.Yellow)Not ready to release:$($PSStyle.Reset)"
            $problems | ForEach-Object { Write-Host "  - $_" }
            Write-Host ''
        }
        Show-Suggestions $state.Latest
        return
    }

    $request = @{}
    switch ($PSCmdlet.ParameterSetName) {
        'Version' { $request.Version = $Version }
        'Bump' { $request.Bump = $Bump; if ($Prerelease) { $request.Prerelease = $Prerelease } }
        'Prerelease' { $request.Prerelease = $Prerelease }
        'Promote' { $request.Promote = $true }
    }
    $next = Get-NextReleaseVersion -Latest $state.Latest @request
    $state.Changelog = Get-ChangelogStatus $next.Tag

    Show-State $state $next
    $problems = @(Get-Problems $state $next)
    if ($problems.Count -gt 0) {
        throw "Cannot release $($next.Tag):`n$(($problems | ForEach-Object { "  - $_" }) -join "`n")"
    }

    if (-not $PSCmdlet.ShouldProcess("$($next.Tag) on $($state.Short) ($Branch)", 'Create and push release tag')) {
        return
    }

    Invoke-Tool git @('tag', '--annotate', $next.Tag, '--message', "Release $($next.Tag)", $state.Head) | Out-Null
    Invoke-Tool git @('push', '--quiet', $Remote, "refs/tags/$($next.Tag)") | Out-Null
    Write-Host "$($PSStyle.Foreground.Green)Pushed tag $($next.Tag) to $Remote.$($PSStyle.Reset)"

    Wait-ReleaseRun $next
}
catch {
    Write-Host "$($PSStyle.Foreground.Red)✖ $($_.Exception.Message)$($PSStyle.Reset)"
    exit 1
}
finally {
    Set-Location $location
}
