<#
.SYNOPSIS
    Pushes a NuGet package to GitHub Packages using dotnet CLI.

.DESCRIPTION
    This script pushes a .nupkg file to a specified GitHub NuGet feed.
    It retrieves the API Key (PAT) via two methods in this order:
    1. Checks for an environment variable named 'GITHUB_NUGET_TOKEN'.
    2. If not found, prompts the user to enter the token interactively (input is masked).

.PARAMETER PackageVersion
    The 3-part version number (e.g., 1.0.0) of the package to push.

.EXAMPLE
    ./push_package.ps1 -PackageVersion "1.0.0"
#>

param (
    [Parameter(Mandatory=$true, Position=0)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$PackageVersion
)

$ErrorActionPreference = "Stop"

# --- CONFIGURATION ---
$GitHubUsername = "DFE-Digital"
$PackageName    = "SUI.Transfer.API.Client"           # Replace with your package ID
$BaseDir        = "./src/SUI.Transfer.API.Client/bin/Debug"   # Directory where packages are built
# ---------------------------------------------

# Construct the expected file path based on version
# Standard naming convention: Name.Version.nupkg
$PackagePath = Join-Path $BaseDir "$PackageName.$PackageVersion.nupkg"

# Validate that the constructed path actually exists
if (-not (Test-Path $PackagePath -PathType Leaf)) {
    Write-Error "Could not find package file at: $PackagePath"
    exit 1
}

function Get-ApiToken {
    # Method 1: Try to retrieve from Environment Variable
    # This is best for CI/CD pipelines or users who export the var in their .zshrc
    $envToken = $env:GITHUB_NUGET_TOKEN

    if (-not [string]::IsNullOrWhiteSpace($envToken)) {
        Write-Host " [INFO] Using API Token from environment variable 'GITHUB_NUGET_TOKEN'." -ForegroundColor Cyan
        return $envToken
    }

    # Method 2: Interactive Prompt
    # This is best for manual runs. -MaskInput hides the characters as you type.
    Write-Host " [WARN] Environment variable 'GITHUB_NUGET_TOKEN' not found." -ForegroundColor Yellow
    $userInput = Read-Host "Please enter your GitHub Personal Access Token (PAT)" -MaskInput

    if ([string]::IsNullOrWhiteSpace($userInput)) {
        Write-Error "No token provided. Exiting."
        exit 1
    }

    return $userInput
}

try {
    # 1. Get the Token safely
    $apiKey = Get-ApiToken

    # 2. Construct the Source URL
    $sourceUrl = "https://nuget.pkg.github.com/$GitHubUsername/index.json"

    Write-Host "`nStarting upload..."
    Write-Host "Package: $PackagePath"
    Write-Host "Source:  $sourceUrl"
    Write-Host "--------------------------------------------------"

    # 3. execute dotnet nuget push
    # We pass the API key directly to the command.
    # --skip-duplicate prevents errors if you accidentally run it on an existing version.
    dotnet nuget push "$PackagePath" `
        --source "$sourceUrl" `
        --api-key "$apiKey" `
        --skip-duplicate

    Write-Host "`n [SUCCESS] Package pushed successfully." -ForegroundColor Green

} catch {
    Write-Host "`n [ERROR] Failed to push package." -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}