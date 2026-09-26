#Requires -Version 7.2
Set-StrictMode -Version Latest

# Version arithmetic for ./release.ps1. Release tags look like v<major>.<minor>.<patch>, optionally followed by
# -<label>.<number> for prereleases (v1.2.0-preview.1, v1.2.0-rc.2). Pure functions, covered by tests/Release.Tests.ps1.

$script:ReleaseVersionPattern = '^v?(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<label>[A-Za-z]+)\.(?<number>[1-9]\d*))?$'

class ReleaseVersion {
    [int] $Major
    [int] $Minor
    [int] $Patch
    # Untyped on purpose: a [string] property turns $null into '', which would make every version a prerelease.
    $Label
    [Nullable[int]] $Number

    [bool] IsPrerelease() { return $null -ne $this.Label }

    [string] Core() { return '{0}.{1}.{2}' -f $this.Major, $this.Minor, $this.Patch }

    [string] ToString() {
        if ($this.IsPrerelease()) { return 'v{0}-{1}.{2}' -f $this.Core(), $this.Label, $this.Number }
        return 'v' + $this.Core()
    }
}

function New-ReleaseVersion {
    param([int] $Major, [int] $Minor, [int] $Patch, [string] $Label, [Nullable[int]] $Number)

    $version = [ReleaseVersion]::new()
    $version.Major = $Major
    $version.Minor = $Minor
    $version.Patch = $Patch
    $version.Label = if ($Label) { $Label } else { $null }
    $version.Number = if ($Label) { $Number } else { $null }
    $version | Add-Member -MemberType ScriptProperty -Name Tag -Value { $this.ToString() } -PassThru
}

function ConvertTo-ReleaseVersion {
    <#
    .SYNOPSIS
    Parses a release version or tag such as 1.2.0, v1.2.0 or v1.2.0-preview.1.
    #>
    [CmdletBinding()]
    [OutputType([ReleaseVersion])]
    param([Parameter(Mandatory, Position = 0)] [AllowEmptyString()] [string] $Text)

    $match = [regex]::Match($Text, $script:ReleaseVersionPattern)
    if (-not $match.Success) {
        throw "'$Text' is not a release version. Use <major>.<minor>.<patch> or <major>.<minor>.<patch>-<label>.<number>, e.g. 1.2.0 or 1.2.0-preview.1."
    }

    $label = $match.Groups['label']
    New-ReleaseVersion `
        -Major $match.Groups['major'].Value -Minor $match.Groups['minor'].Value -Patch $match.Groups['patch'].Value `
        -Label ($label.Success ? $label.Value : $null) `
        -Number ($label.Success ? [int]$match.Groups['number'].Value : $null)
}

function Compare-ReleaseVersion {
    <#
    .SYNOPSIS
    Compares two release versions by SemVer precedence: returns -1, 0 or 1.
    #>
    [CmdletBinding()]
    [OutputType([int])]
    param([Parameter(Mandatory, Position = 0)] $Left, [Parameter(Mandatory, Position = 1)] $Right)

    foreach ($part in 'Major', 'Minor', 'Patch') {
        if ($Left.$part -ne $Right.$part) { return [Math]::Sign($Left.$part - $Right.$part) }
    }

    # A stable release sorts above any prerelease of the same core version.
    if ($Left.IsPrerelease() -ne $Right.IsPrerelease()) { return $Left.IsPrerelease() ? -1 : 1 }
    if (-not $Left.IsPrerelease()) { return 0 }

    $labelOrder = [string]::CompareOrdinal($Left.Label, $Right.Label)
    if ($labelOrder -ne 0) { return [Math]::Sign($labelOrder) }
    return [Math]::Sign($Left.Number - $Right.Number)
}

function Get-LatestReleaseVersion {
    <#
    .SYNOPSIS
    Returns the highest release version among git tags, or $null. Tags that are not release versions are ignored.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)] [AllowEmptyCollection()] [string[]] $Tags)

    $latest = $null
    foreach ($tag in $Tags) {
        if ($tag -notmatch $script:ReleaseVersionPattern -or -not $tag.StartsWith('v')) { continue }
        $version = ConvertTo-ReleaseVersion $tag
        if ($null -eq $latest -or (Compare-ReleaseVersion $version $latest) -gt 0) { $latest = $version }
    }
    return $latest
}

function Get-NextReleaseVersion {
    <#
    .SYNOPSIS
    Computes the next release version from the latest one and exactly one of -Version, -Bump (optionally with
    -Prerelease), -Prerelease or -Promote. Throws when the request makes no sense for the latest release.
    #>
    [CmdletBinding()]
    param(
        [AllowNull()] $Latest,
        [string] $Version,
        [ValidateSet('Major', 'Minor', 'Patch')] [string] $Bump,
        [ValidatePattern('^[A-Za-z]+$')] [string] $Prerelease,
        [switch] $Promote
    )

    $modes = @($Version, $Bump, ($Prerelease -and -not $Bump), $Promote.IsPresent) | Where-Object { $_ }
    if (@($modes).Count -ne 1) {
        throw 'Specify exactly one of -Version, -Bump (optionally with -Prerelease), -Prerelease or -Promote.'
    }

    if ($Version) {
        $next = ConvertTo-ReleaseVersion $Version
    }
    elseif ($Bump) {
        $base = $Latest ?? (New-ReleaseVersion -Major 0 -Minor 0 -Patch 0)
        $next = switch ($Bump) {
            'Major' { New-ReleaseVersion -Major ($base.Major + 1) -Minor 0 -Patch 0 }
            'Minor' { New-ReleaseVersion -Major $base.Major -Minor ($base.Minor + 1) -Patch 0 }
            'Patch' { New-ReleaseVersion -Major $base.Major -Minor $base.Minor -Patch ($base.Patch + 1) }
        }
        if ($Prerelease) {
            $next = New-ReleaseVersion -Major $next.Major -Minor $next.Minor -Patch $next.Patch -Label $Prerelease -Number 1
        }
    }
    elseif ($Prerelease) {
        if ($null -eq $Latest) {
            throw "There is no release yet; start a prerelease with -Bump, e.g. -Bump Minor -Prerelease $Prerelease."
        }
        if (-not $Latest.IsPrerelease()) {
            throw "The latest release $($Latest.Tag) is not a prerelease; start the next one with -Bump, e.g. -Bump Minor -Prerelease $Prerelease."
        }
        $number = $Latest.Label -ceq $Prerelease ? $Latest.Number + 1 : 1
        $next = New-ReleaseVersion -Major $Latest.Major -Minor $Latest.Minor -Patch $Latest.Patch -Label $Prerelease -Number $number
    }
    else {
        if ($null -eq $Latest -or -not $Latest.IsPrerelease()) {
            $current = $null -eq $Latest ? 'there is no release yet' : "the latest release $($Latest.Tag) is already stable"
            throw "There is nothing to promote: $current."
        }
        $next = New-ReleaseVersion -Major $Latest.Major -Minor $Latest.Minor -Patch $Latest.Patch
    }

    if ($null -ne $Latest -and (Compare-ReleaseVersion $next $Latest) -le 0) {
        throw "$($next.Tag) is not higher than the latest release $($Latest.Tag)."
    }
    return $next
}

Export-ModuleMember -Function ConvertTo-ReleaseVersion, Compare-ReleaseVersion, Get-LatestReleaseVersion, Get-NextReleaseVersion
