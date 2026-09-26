#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

# Tests for the version logic behind ./release.ps1. Run with: Invoke-Pester ./tests/Release.Tests.ps1

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '..' 'build' 'Release.psm1') -Force
}

Describe 'ConvertTo-ReleaseVersion' {
    It 'parses <Text>' -ForEach @(
        @{ Text = 'v1.2.3'; Major = 1; Minor = 2; Patch = 3; Label = $null; Number = $null; Tag = 'v1.2.3' }
        @{ Text = '1.2.3'; Major = 1; Minor = 2; Patch = 3; Label = $null; Number = $null; Tag = 'v1.2.3' }
        @{ Text = 'v0.1.0-preview.2'; Major = 0; Minor = 1; Patch = 0; Label = 'preview'; Number = 2; Tag = 'v0.1.0-preview.2' }
        @{ Text = '10.20.30-rc.11'; Major = 10; Minor = 20; Patch = 30; Label = 'rc'; Number = 11; Tag = 'v10.20.30-rc.11' }
    ) {
        $version = ConvertTo-ReleaseVersion $Text
        $version.Major | Should -Be $Major
        $version.Minor | Should -Be $Minor
        $version.Patch | Should -Be $Patch
        $version.Label | Should -Be $Label
        $version.Number | Should -Be $Number
        $version.Tag | Should -Be $Tag
        $version.ToString() | Should -Be $Tag
    }

    It 'rejects <Text>' -ForEach @(
        @{ Text = '' }
        @{ Text = 'v1.2' }
        @{ Text = 'v1.2.3.4' }
        @{ Text = 'v01.2.3' }
        @{ Text = 'v1.2.3-preview' }
        @{ Text = 'v1.2.3-preview.0' }
        @{ Text = 'v1.2.3-pre-view.1' }
        @{ Text = 'v1.2.3+build.5' }
        @{ Text = 'latest' }
    ) {
        { ConvertTo-ReleaseVersion $Text } | Should -Throw '*not a release version*'
    }
}

Describe 'Compare-ReleaseVersion' {
    It '<Left> is <Expected> <Right>' -ForEach @(
        @{ Left = 'v1.2.3'; Right = 'v1.2.3'; Expected = 0 }
        @{ Left = 'v1.2.4'; Right = 'v1.2.3'; Expected = 1 }
        @{ Left = 'v1.3.0'; Right = 'v1.2.9'; Expected = 1 }
        @{ Left = 'v2.0.0'; Right = 'v1.9.9'; Expected = 1 }
        @{ Left = 'v1.2.0'; Right = 'v1.2.0-rc.1'; Expected = 1 }
        @{ Left = 'v1.2.0-rc.1'; Right = 'v1.2.0-preview.9'; Expected = 1 }
        @{ Left = 'v1.2.0-preview.10'; Right = 'v1.2.0-preview.9'; Expected = 1 }
        @{ Left = 'v1.2.0-preview.1'; Right = 'v1.1.9'; Expected = 1 }
        @{ Left = 'v1.1.9'; Right = 'v1.2.0-preview.1'; Expected = -1 }
    ) {
        Compare-ReleaseVersion (ConvertTo-ReleaseVersion $Left) (ConvertTo-ReleaseVersion $Right) | Should -Be $Expected
    }
}

Describe 'Get-LatestReleaseVersion' {
    It 'returns the highest release tag, ignoring tags that are not release versions' {
        $latest = Get-LatestReleaseVersion -Tags @('v0.9.0', 'v1.2.0-preview.2', 'v1.1.0', 'nightly', 'v1.2.0-preview.10', 'v1.0')
        $latest.Tag | Should -Be 'v1.2.0-preview.10'
    }

    It 'returns $null when there are no release tags' {
        Get-LatestReleaseVersion -Tags @() | Should -BeNullOrEmpty
        Get-LatestReleaseVersion -Tags @('nightly') | Should -BeNullOrEmpty
    }
}

Describe 'Get-NextReleaseVersion' {
    Context 'with -Bump' {
        It '<Latest> + <Bump> = <Expected>' -ForEach @(
            @{ Latest = 'v1.2.3'; Bump = 'Patch'; Expected = 'v1.2.4' }
            @{ Latest = 'v1.2.3'; Bump = 'Minor'; Expected = 'v1.3.0' }
            @{ Latest = 'v1.2.3'; Bump = 'Major'; Expected = 'v2.0.0' }
            @{ Latest = 'v1.2.0-preview.2'; Bump = 'Minor'; Expected = 'v1.3.0' }
            @{ Latest = $null; Bump = 'Minor'; Expected = 'v0.1.0' }
            @{ Latest = $null; Bump = 'Patch'; Expected = 'v0.0.1' }
        ) {
            $latestVersion = if ($Latest) { ConvertTo-ReleaseVersion $Latest } else { $null }
            (Get-NextReleaseVersion -Latest $latestVersion -Bump $Bump).Tag | Should -Be $Expected
        }

        It '<Latest> + <Bump> -Prerelease <Label> = <Expected>' -ForEach @(
            @{ Latest = 'v1.1.0'; Bump = 'Minor'; Label = 'preview'; Expected = 'v1.2.0-preview.1' }
            @{ Latest = 'v1.1.0'; Bump = 'Major'; Label = 'rc'; Expected = 'v2.0.0-rc.1' }
            @{ Latest = $null; Bump = 'Minor'; Label = 'preview'; Expected = 'v0.1.0-preview.1' }
        ) {
            $latestVersion = if ($Latest) { ConvertTo-ReleaseVersion $Latest } else { $null }
            (Get-NextReleaseVersion -Latest $latestVersion -Bump $Bump -Prerelease $Label).Tag | Should -Be $Expected
        }
    }

    Context 'with -Prerelease only' {
        It '<Latest> -Prerelease <Label> = <Expected>' -ForEach @(
            @{ Latest = 'v1.2.0-preview.1'; Label = 'preview'; Expected = 'v1.2.0-preview.2' }
            @{ Latest = 'v1.2.0-preview.9'; Label = 'preview'; Expected = 'v1.2.0-preview.10' }
            @{ Latest = 'v1.2.0-preview.3'; Label = 'rc'; Expected = 'v1.2.0-rc.1' }
        ) {
            (Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion $Latest) -Prerelease $Label).Tag | Should -Be $Expected
        }

        It 'refuses when the latest release is stable' {
            { Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion 'v1.1.0') -Prerelease 'preview' } |
                Should -Throw '*is not a prerelease*-Bump*'
        }

        It 'refuses when there is no release yet' {
            { Get-NextReleaseVersion -Latest $null -Prerelease 'preview' } | Should -Throw '*no release yet*-Bump*'
        }

        It 'refuses a label that sorts below the current one' {
            { Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion 'v1.2.0-rc.1') -Prerelease 'preview' } |
                Should -Throw '*not higher than*'
        }
    }

    Context 'with -Promote' {
        It 'promotes <Latest> to <Expected>' -ForEach @(
            @{ Latest = 'v1.2.0-preview.2'; Expected = 'v1.2.0' }
            @{ Latest = 'v2.0.0-rc.4'; Expected = 'v2.0.0' }
        ) {
            (Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion $Latest) -Promote).Tag | Should -Be $Expected
        }

        It 'refuses when the latest release is already stable' {
            { Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion 'v1.2.0') -Promote } | Should -Throw '*nothing to promote*'
        }

        It 'refuses when there is no release yet' {
            { Get-NextReleaseVersion -Latest $null -Promote } | Should -Throw '*nothing to promote*'
        }
    }

    Context 'with -Version' {
        It 'accepts a version higher than the latest release' {
            (Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion 'v1.1.0') -Version '1.3.0-rc.1').Tag | Should -Be 'v1.3.0-rc.1'
        }

        It 'accepts any valid version when there is no release yet' {
            (Get-NextReleaseVersion -Latest $null -Version 'v0.1.0').Tag | Should -Be 'v0.1.0'
        }

        It 'refuses <Version> after <Latest>' -ForEach @(
            @{ Latest = 'v1.1.0'; Version = 'v1.1.0' }
            @{ Latest = 'v1.1.0'; Version = 'v1.0.9' }
            @{ Latest = 'v1.2.0'; Version = 'v1.2.0-preview.5' }
        ) {
            { Get-NextReleaseVersion -Latest (ConvertTo-ReleaseVersion $Latest) -Version $Version } | Should -Throw '*not higher than*'
        }
    }

    It 'requires exactly one of -Version, -Bump, -Prerelease or -Promote' {
        { Get-NextReleaseVersion -Latest $null } | Should -Throw '*Specify*'
        { Get-NextReleaseVersion -Latest $null -Version 'v1.0.0' -Promote } | Should -Throw '*Specify*'
    }
}
