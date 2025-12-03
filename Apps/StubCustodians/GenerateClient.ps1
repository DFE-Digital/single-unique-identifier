<#
.SYNOPSIS
    Generates the strongly-typed Custodian Data API client from StubCustodians endpoints.

.DESCRIPTION
    This script uses NSwag to generate C# client classes and models
    from from StubCustodians endpoints. The output is placed
    in the API Client project directory, ready for packing into a NuGet package.

.EXAMPLE
    pwsh ./GenerateClient.ps1
#>

# Get the folder where this script lives
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Paths
$ApiProject = Join-Path $ScriptDir "src/SUI.StubCustodians.API/SUI.StubCustodians.API.csproj"
$OutputSpec = Join-Path $ScriptDir "src/SUI.Custodians.API.Client/SUI.Custodians.API.json"
$NswagConfig = Join-Path $ScriptDir "nswag.json"

Write-Host "Building StubCustodians API..."
dotnet build $ApiProject --configuration Release

# Generate OpenAPI JSON from compiled DLL
$DllPath = Join-Path $ScriptDir "src/SUI.StubCustodians.API/bin/Release/net9.0/SUI.StubCustodians.API.dll"

if (-Not (Test-Path $DllPath)) {
    Write-Error "DLL not found: $DllPath"
    exit 1
}

# Check for global swagger tool
if (-not (Get-Command swagger -ErrorAction SilentlyContinue)) {
    Write-Host "Swashbuckle CLI not found globally. Installing..."
    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 9.0.6
    # Ensure the global tools path is in the PATH
    $env:PATH += ";" + "$HOME/.dotnet/tools"
}

Write-Host "Generating OpenAPI JSON from DLL..."
swagger tofile --output $OutputSpec $DllPath v1

if (-Not (Test-Path $OutputSpec)) {
    Write-Error "OpenAPI JSON generation failed."
    exit 1
}

Write-Host "Generating client using NSwag..."
dotnet nswag run $NswagConfig

Write-Host "Client generation complete!"