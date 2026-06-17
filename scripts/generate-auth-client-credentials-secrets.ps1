<#
    .DESCRIPTION
    Script for generating the values/templates to use for the AUTH_CLIENT_IDS_JSON_MAP and AUTH_CLIENT_SECRETS_JSON_MAP secrets.
    This is for the `AuthClientCredentials` configuration functionality that enables overriding the Client IDs and Secrets in the sample data.

    .EXAMPLE
    (Having already run `dotnet tool restore` as a one-off prerequisite. Using the dotnet tool alleviates PowerShell execution policy issues.)

    To generate secret values:
    dotnet pwsh ./scripts/generate-auth-client-credentials-secrets.ps1

    To generate template values:
    dotnet pwsh ./scripts/generate-auth-client-credentials-secrets.ps1 -TemplateMode
#>

param(
    [switch]$TemplateMode
)

$ErrorActionPreference = "Stop"

$authClientsFilePath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, "..\Data\auth-clients-inbound.json"))
$authClients = Get-Content -Path $authClientsFilePath -Raw | ConvertFrom-Json

$clientIdsMap = [ordered]@{}
$clientSecretMap = [ordered]@{}

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    foreach ($client in $authClients.clients) {
        if ($TemplateMode) {
            $clientIdsMap[$client.clientId] = ""
            $clientSecretMap[$client.clientId] = ""
        } else {
            $clientIdsMap[$client.clientId] = "$(New-Guid)"

            $randomBytes = New-Object byte[] 32
            $rng.GetBytes($randomBytes)
            $base64Secret = [System.Convert]::ToBase64String($randomBytes).TrimEnd('=')
            $clientSecretMap[$client.clientId] = $base64Secret
        }
    }
} finally {
    $rng.Dispose()
}

Write-Host "AUTH_CLIENT_IDS_JSON_MAP:" -ForegroundColor Cyan
Write-Host "$($clientIdsMap | ConvertTo-Json -Compress)" -ForegroundColor Green

Write-Host "AUTH_CLIENT_SECRETS_JSON_MAP:" -ForegroundColor Cyan
Write-Host "$($clientSecretMap | ConvertTo-Json -Compress)" -ForegroundColor Green
