<#
.SYNOPSIS
    Builds the Custodians API and generates the strongly-typed client.

.DESCRIPTION
    1. Builds the API project.
    2. Uses the pre-generated OpenAPI JSON to generate C# client classes via NSwag.
#>

# Get the folder where this script lives
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Paths
$ApiProject = Join-Path $ScriptDir "src/SUI.StubCustodians.API/SUI.StubCustodians.API.csproj"
$OpenApiSpec = Join-Path $ScriptDir "src/SUI.Custodians.API.Client/custodians-openapi-generated.json"
$NswagConfig = Join-Path $ScriptDir "nswag.json"

# Step 1: Build the API project
Write-Host "Building Custodians API..."
dotnet build $ApiProject --configuration Release

# Optional: verify JSON exists
if (-Not (Test-Path $OpenApiSpec)) {
    Write-Error "OpenAPI JSON not found: $OpenApiSpec"
    exit 1
}

# Step 2: Generate client via NSwag
Write-Host "Generating client from OpenAPI JSON..."
dotnet nswag run $NswagConfig

Write-Host "Client generation complete!"
