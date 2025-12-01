<#
.SYNOPSIS
    Builds, packs, and pushes the Custodians API Client NuGet package to GitHub Packages.

.DESCRIPTION
    This script:
      1. Builds the API client project.
      2. Packs it into a .nupkg using the specified version + configuration.
      3. Pushes the package to GitHub Packages.
    It retrieves the GitHub PAT from:
      - Environment variable GITHUB_NUGET_TOKEN (preferred)
      - Or interactively (masked input)

.PARAMETER PackageVersion
    The Semantic Version (e.g. "1.0.0")

.PARAMETER Configuration
    Build configuration (Debug or Release). Default is Release.

.EXAMPLE
    ./PushClient.ps1 -PackageVersion 1.0.0 -Configuration Release
#>

param (
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$PackageVersion,

    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# --- CONFIGURATION ---
$GitHubUsername = "DFE-Digital"
$PackageName    = "SUI.Custodians.API.Client"
$ProjectPath    = "./Apps/Custodians/src/SUI.Custodians.API.Client/SUI.Custodians.API.Client.csproj"
$BaseDir        = "./Apps/Custodians/src/SUI.Custodians.API.Client/bin/$Configuration"
# ---------------------------------------------

# ---- STEP 1: BUILD PROJECT ----
Write-Host "`nBuilding client project ($Configuration)..." -ForegroundColor Cyan
dotnet build $ProjectPath -c $Configuration

# ---- STEP 2: PACK PROJECT ----
Write-Host "`nPacking NuGet package version $PackageVersion ($Configuration)..." -ForegroundColor Cyan

dotnet pack $ProjectPath `
    -c $Configuration `
    -p:PackageVersion=$PackageVersion `
    --no-build

# Construct expected package file path
$PackagePath = Join-Path $BaseDir "$PackageName.$PackageVersion.nupkg"

if (-not (Test-Path $PackagePath -PathType Leaf)) {
    Write-Error "Could not find package file at: $PackagePath"
    exit 1
}

# ---- STEP 3: RETRIEVE TOKEN ----
function Get-ApiToken {
    $envToken = $env:GITHUB_NUGET_TOKEN

    if (-not [string]::IsNullOrWhiteSpace($envToken)) {
        Write-Host " [INFO] Using API Token from environment variable 'GITHUB_NUGET_TOKEN'." -ForegroundColor Cyan
        return $envToken
    }

    Write-Host " [WARN] Environment variable 'GITHUB_NUGET_TOKEN' not found." -ForegroundColor Yellow
    $userInput = Read-Host "Please enter your GitHub Personal Access Token (PAT)" -MaskInput

    if ([string]::IsNullOrWhiteSpace($userInput)) {
        Write-Error "No token provided. Exiting."
        exit 1
    }

    return $userInput
}

try {
    $apiKey = Get-ApiToken

    $sourceUrl = "https://nuget.pkg.github.com/$GitHubUsername/index.json"

    Write-Host "`nStarting upload..."
    Write-Host "Package: $PackagePath"
    Write-Host "Source:  $sourceUrl"
    Write-Host "--------------------------------------------------"

    dotnet nuget push "$PackagePath" `
        --source "$sourceUrl" `
        --api-key "$apiKey" `
        --skip-duplicate

    Write-Host "`n [SUCCESS] Package pushed successfully." -ForegroundColor Green

}
catch {
    Write-Host "`n [ERROR] Failed to push package." -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}
