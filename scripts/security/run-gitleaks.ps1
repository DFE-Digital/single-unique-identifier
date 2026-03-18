param(
    [ValidateSet("PreCommit", "Baseline")]
    [string]$Mode = "PreCommit",

    [switch]$NoInstall
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$configPath = Join-Path $repoRoot ".gitleaks.toml"
$baselinePath = Join-Path $repoRoot ".gitleaks.baseline.json"
$gitleaksVersion = "8.30.0"

function Get-PlatformSpec {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    $platform = $null
    $archiveExtension = $null
    $binaryName = $null

    if ($IsMacOS) {
        switch ($architecture) {
            "arm64" { $platform = "darwin_arm64"; break }
            "x64" { $platform = "darwin_x64"; break }
            default {
                throw "Automatic GitLeaks installation on macOS supports x64 and arm64 only. Install GitLeaks $gitleaksVersion manually and ensure it is on PATH for architecture '$architecture'."
            }
        }
    }

    if ($IsLinux) {
        switch ($architecture) {
            "arm64" { $platform = "linux_arm64"; break }
            "x64" { $platform = "linux_x64"; break }
            default {
                throw "Automatic GitLeaks installation on Linux supports x64 and arm64 only. Install GitLeaks $gitleaksVersion manually and ensure it is on PATH for architecture '$architecture'."
            }
        }
    }

    if ($IsWindows) {
        if ($architecture -eq "x64") {
            $platform = "windows_x64"
        } else {
            throw "Automatic GitLeaks installation on Windows currently supports x64 only. Install GitLeaks $gitleaksVersion manually and ensure it is on PATH for architecture '$architecture'."
        }
    }

    if (-not $platform) {
        throw "Unsupported operating system for GitLeaks installation: $([System.Runtime.InteropServices.RuntimeInformation]::OSDescription)"
    }

    if ($platform.StartsWith("windows_", [System.StringComparison]::Ordinal)) {
        $archiveExtension = "zip"
        $binaryName = "gitleaks.exe"
    } else {
        $archiveExtension = "tar.gz"
        $binaryName = "gitleaks"
    }

    return @{
        CacheKey = $platform.Replace("_", "-")
        ArchiveName = "gitleaks_${gitleaksVersion}_${platform}.${archiveExtension}"
        BinaryName = $binaryName
    }
}

function Get-CacheRoot {
    if ($IsWindows) {
        return Join-Path $env:LOCALAPPDATA "single-unique-identifier/tools"
    }

    if ($env:XDG_CACHE_HOME) {
        return Join-Path $env:XDG_CACHE_HOME "single-unique-identifier/tools"
    }

    return Join-Path $HOME ".cache/single-unique-identifier/tools"
}

function Get-VersionFromCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandPath
    )

    try {
        $versionOutput = & $CommandPath version 2>$null
        $match = [regex]::Match(($versionOutput -join "`n"), "\d+\.\d+\.\d+")
        if ($match.Success) {
            return $match.Value
        }
    } catch {
        return $null
    }

    return $null
}

function Install-Gitleaks {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$PlatformSpec,

        [Parameter(Mandatory = $true)]
        [string]$InstallDirectory
    )

    $archivePath = Join-Path ([System.IO.Path]::GetTempPath()) $PlatformSpec.ArchiveName
    $downloadUrl = "https://github.com/gitleaks/gitleaks/releases/download/v$gitleaksVersion/$($PlatformSpec.ArchiveName)"

    Write-Host "Downloading GitLeaks $gitleaksVersion from $downloadUrl"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

    New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null

    if ($archivePath.EndsWith(".zip", [System.StringComparison]::OrdinalIgnoreCase)) {
        Expand-Archive -Path $archivePath -DestinationPath $InstallDirectory -Force
    } else {
        tar -xzf $archivePath -C $InstallDirectory
    }

    Remove-Item $archivePath -Force

    $binaryPath = Join-Path $InstallDirectory $PlatformSpec.BinaryName
    if (-not (Test-Path $binaryPath)) {
        throw "GitLeaks binary was not found after extraction at $binaryPath."
    }

    if (-not $IsWindows) {
        chmod +x $binaryPath
    }

    return $binaryPath
}

function Get-GitleaksCommand {
    $pathCommand = Get-Command gitleaks -ErrorAction SilentlyContinue
    if ($pathCommand) {
        $pathVersion = Get-VersionFromCommand -CommandPath $pathCommand.Source
        if ($pathVersion -eq $gitleaksVersion) {
            return $pathCommand.Source
        }

        if ($pathVersion) {
            Write-Host "Ignoring GitLeaks $pathVersion on PATH and using pinned GitLeaks $gitleaksVersion instead."
        }
    }

    $platformSpec = Get-PlatformSpec
    $installDirectory = Join-Path (Get-CacheRoot) "gitleaks/$gitleaksVersion/$($platformSpec.CacheKey)"
    $binaryPath = Join-Path $installDirectory $platformSpec.BinaryName

    if (Test-Path $binaryPath) {
        return $binaryPath
    }

    if ($NoInstall) {
        throw "GitLeaks $gitleaksVersion is required but not installed. Install it manually or rerun without -NoInstall."
    }

    return Install-Gitleaks -PlatformSpec $platformSpec -InstallDirectory $installDirectory
}

function Invoke-Gitleaks {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandPath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $CommandPath @Arguments
    return $LASTEXITCODE
}

if ($env:SUI_SKIP_GITLEAKS -eq "1") {
    Write-Host "Skipping GitLeaks because SUI_SKIP_GITLEAKS=1."
    exit 0
}

if (-not (Test-Path $configPath)) {
    throw "GitLeaks config not found at $configPath."
}

Push-Location $repoRoot

try {
    $gitleaksCommand = Get-GitleaksCommand

    if ($Mode -eq "Baseline") {
        $baselineArguments = @(
            "git",
            "--config", $configPath,
            "--report-format", "json",
            "--report-path", $baselinePath
        )

        $exitCode = Invoke-Gitleaks -CommandPath $gitleaksCommand -Arguments $baselineArguments
        if ($exitCode -ne 0) {
            exit $exitCode
        }

        Write-Host "Wrote GitLeaks baseline to $baselinePath"
        exit 0
    }

    $preCommitArguments = @(
        "git",
        "--pre-commit",
        "--staged",
        "--redact",
        "--config", $configPath
    )

    if (Test-Path $baselinePath) {
        $preCommitArguments += @("--baseline-path", $baselinePath)
    }

    $exitCode = Invoke-Gitleaks -CommandPath $gitleaksCommand -Arguments $preCommitArguments
    if ($exitCode -eq 0) {
        exit 0
    }

    Write-Host ""
    Write-Host "GitLeaks found potential secrets in staged changes."
    Write-Host "If the finding is expected, update .gitleaks.toml or regenerate .gitleaks.baseline.json intentionally."
    Write-Host "For urgent commits only, rerun with SUI_SKIP_GITLEAKS=1."
    exit $exitCode
} finally {
    Pop-Location
}
