<#
    .DESCRIPTION
    Sends a realistic "pds-record-change-2" MNS (Multicast Notification Service) event notification
    into a local MESH mailbox (see mesh-sandbox: https://github.com/NHSDigital/mesh-sandbox), without
    needing to run the NotificationService app. Mirrors NhsMeshAuthHandler in
    Apps/NotificationService/src/SUI.NotificationService.Mesh, computing the NHSMESH authorization
    header the same way.

    The payload is a FHIR Bundle (type: history) wrapping a Parameters resource conforming to the
    R4 Subscriptions Backport "SubscriptionStatus" profile that NHS Digital publishes for this event.
    See: https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event

    Requires PowerShell 7+ (uses -SkipCertificateCheck for the sandbox's self-signed certificate).

    .EXAMPLE
    dotnet pwsh ./scripts/send-mesh-test-message.ps1

    .EXAMPLE
    Send to a different recipient mailbox with a specific NHS number:
    dotnet pwsh ./scripts/send-mesh-test-message.ps1 -To X26ABC2 -NhsNumber 9912003888
#>

param(
    [string]$MailboxBaseUrl = "https://localhost:8700",
    [string]$MailboxId = "X26ABC1",
    [string]$MailboxPassword = "password",
    [string]$SharedKey = "TestKey",
    [string]$To = $MailboxId,
    [string]$WorkflowId = "PDSRECORDCHANGE_2",
    [string]$NhsNumber = "9912003888",
    [string]$VersionId = "4",
    [string]$EventNumber = "1"
)

$ErrorActionPreference = "Stop"

function New-MeshAuthorizationHeader {
    param(
        [Parameter(Mandatory = $true)][string]$MailboxId,
        [Parameter(Mandatory = $true)][string]$MailboxPassword,
        [Parameter(Mandatory = $true)][string]$SharedKey
    )

    $nonce = [guid]::NewGuid().ToString()
    $nonceCount = 1
    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmm")

    $message = "$($MailboxId):$($nonce):$($nonceCount):$($MailboxPassword):$($timestamp)"

    $hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($SharedKey))
    try {
        $hashBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($message))
    } finally {
        $hmac.Dispose()
    }
    $hash = ([System.BitConverter]::ToString($hashBytes) -replace '-', '').ToLowerInvariant()

    return "NHSMESH $($MailboxId):$($nonce):$($nonceCount):$($timestamp):$($hash)"
}

function New-PdsRecordChangeNotification {
    param(
        [Parameter(Mandatory = $true)][string]$NhsNumber,
        [Parameter(Mandatory = $true)][string]$VersionId,
        [Parameter(Mandatory = $true)][string]$EventNumber
    )

    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    $subscriptionId = [guid]::NewGuid().ToString()
    $bundleId = [guid]::NewGuid().ToString()
    $parametersId = [guid]::NewGuid().ToString()
    $subscriptionReference = "https://api.service.nhs.uk/multicast-notification-service/subscriptions/$subscriptionId"

    $templatePath = Join-Path $PSScriptRoot "pds-record-change-2-notification.template.json"
    $payload = Get-Content -Path $templatePath -Raw

    $replacements = [ordered]@{
        "{{BUNDLE_ID}}"              = $bundleId
        "{{TIMESTAMP}}"              = $timestamp
        "{{PARAMETERS_ID}}"          = $parametersId
        "{{SUBSCRIPTION_REFERENCE}}" = $subscriptionReference
        "{{EVENT_NUMBER}}"           = $EventNumber
        "{{NHS_NUMBER}}"             = $NhsNumber
        "{{VERSION_ID}}"             = $VersionId
    }

    foreach ($placeholder in $replacements.Keys) {
        $payload = $payload.Replace($placeholder, $replacements[$placeholder])
    }

    return $payload
}

$payload = New-PdsRecordChangeNotification -NhsNumber $NhsNumber -VersionId $VersionId -EventNumber $EventNumber
$authorization = New-MeshAuthorizationHeader -MailboxId $MailboxId -MailboxPassword $MailboxPassword -SharedKey $SharedKey

$headers = @{
    "Authorization"  = $authorization
    "mex-From"       = $MailboxId
    "mex-To"         = $To
    "mex-WorkflowID" = $WorkflowId
    "mex-FileName"   = "pds-record-change-2.json"
    "Accept"         = "application/vnd.mesh.v2+json"
}

$uri = "$MailboxBaseUrl/messageexchange/$MailboxId/outbox"

Write-Host "Sending pds-record-change-2 notification to $uri (mex-To: $To, mex-WorkflowID: $WorkflowId)" -ForegroundColor Cyan

$result = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $payload -ContentType "application/fhir+json" -SkipCertificateCheck

Write-Host "MESH message sent!" -ForegroundColor Green
$result | ConvertTo-Json -Depth 10 | Write-Host
