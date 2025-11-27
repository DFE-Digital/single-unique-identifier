<#
.SYNOPSIS
    Generates the strongly-typed Custodian Data API client from a provided OpenAPI spec.

.DESCRIPTION
    This script uses NSwag to generate C# client classes and models
    from a `spec.yml` OpenAPI specification. The output is placed
    in the API Client project directory, ready for packing into a NuGet package.

.EXAMPLE
    pwsh ./GenerateClient.ps1
#>

# Get the folder where this script lives
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Run NSwag from the script folder
nswag run "$ScriptDir/nswag.json"
