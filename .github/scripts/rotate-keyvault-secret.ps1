<#
.SYNOPSIS
Resolves configuration and executes helper commands for the `rotate-keyvault-secret` workflow.

.DESCRIPTION
This script is the single helper entrypoint for the secret-rotation workflow. It reads a
manifest-driven definition of supported secrets, resolves environment-specific context,
and performs the orchestration steps that would otherwise make the workflow YAML hard
to maintain.

The supported commands intentionally map to workflow phases:
- `matrix`: decide which `{ environment, secret_name }` pairs the workflow should run
- `context`: resolve manifest/profile data into concrete resource names and Terraform inputs
- `inspect`: read current secret metadata and decide whether scheduled rotation is due
- `plan`: build the targeted Terraform plan for the selected secret
- `apply-config`: refresh dependent configuration and restart the app if required
- `summary`: write a concise workflow summary without exposing secret values
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('matrix', 'context', 'inspect', 'plan', 'apply-config', 'summary')]
    [string]$Command
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSNativeCommandUseErrorActionPreference = $true
}

$manifestPath = if ($env:MANIFEST_PATH) { $env:MANIFEST_PATH } else { '.github/rotate-keyvault-secret.manifest.json' }

function Get-EnvironmentValue {
    param(
        [string]$Name,
        [string]$Default = '',
        [switch]$Required
    )

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        if ($Required) {
            throw "$Name is required."
        }

        return $Default
    }

    return $value
}

function Add-GitHubOutputValue {
    param(
        [string]$Name,
        [string]$Value
    )

    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "$Name=$Value"
}

function Add-GitHubOutputMultiline {
    param(
        [string]$Name,
        [string[]]$Values
    )

    $delimiter = "__$($Name.ToUpperInvariant())__"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "$Name<<$delimiter"
    foreach ($value in $Values) {
        Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value $value
    }
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value $delimiter
}

function Get-MultilineEnvItems {
    param([string]$Name)

    $value = Get-EnvironmentValue -Name $Name
    if ([string]::IsNullOrWhiteSpace($value)) {
        return @()
    }

    return @(
        $value -split "(`r`n|`n|`r)" |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function Join-Hashtable {
    param(
        [hashtable]$Base = @{},
        [hashtable]$Override = @{}
    )

    if ($null -eq $Base) {
        $Base = @{}
    }

    if ($null -eq $Override) {
        $Override = @{}
    }

    $merged = @{}

    foreach ($key in $Base.Keys) {
        $merged[$key] = $Base[$key]
    }

    foreach ($key in $Override.Keys) {
        if (
            $merged.ContainsKey($key) -and
            $merged[$key] -is [hashtable] -and
            $Override[$key] -is [hashtable]
        ) {
            $merged[$key] = Join-Hashtable -Base $merged[$key] -Override $Override[$key]
        }
        else {
            $merged[$key] = $Override[$key]
        }
    }

    return $merged
}

function Get-Manifest {
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Required file not found: $manifestPath"
    }

    return Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable -Depth 32
}

function Format-Template {
    param(
        [string]$Template,
        [hashtable]$Context
    )

    $rendered = $Template
    foreach ($key in $Context.Keys) {
        $rendered = $rendered.Replace("{$key}", [string]$Context[$key])
    }
    return $rendered
}

function Get-RotationMatrix {
    Get-EnvironmentValue -Name 'GITHUB_EVENT_NAME' -Required | Out-Null
    Get-EnvironmentValue -Name 'GITHUB_OUTPUT' -Required | Out-Null

    $manifest = Get-Manifest
    $secrets = $manifest['secrets']
    $eventName = $env:GITHUB_EVENT_NAME
    $targetEnvironment = $env:TARGET_ENVIRONMENT
    $targetSecretName = $env:TARGET_SECRET_NAME

    if ($eventName -eq 'workflow_dispatch') {
        if (-not $secrets.ContainsKey($targetSecretName)) {
            throw "Unsupported secret name: $targetSecretName"
        }
        if ([string]::IsNullOrWhiteSpace($targetEnvironment)) {
            throw 'TARGET_ENVIRONMENT is required for workflow_dispatch.'
        }

        $matrix = @(@{ environment = $targetEnvironment; secret_name = $targetSecretName })
        Add-GitHubOutputValue -Name 'matrix' -Value ($matrix | ConvertTo-Json -Compress -AsArray)
        Add-GitHubOutputValue -Name 'has_targets' -Value 'true'
        return
    }

    # Scheduled runs expand from the manifest so new scheduled secrets can be added
    # without editing the workflow matrix in YAML.
    $matrix = @()
    foreach ($secretName in ($secrets.Keys | Sort-Object)) {
        $secretDefinition = $secrets[$secretName]
        $schedule = if ($secretDefinition.ContainsKey('schedule')) { $secretDefinition['schedule'] } else { @{} }
        $isEnabled = if ($schedule.ContainsKey('enabled')) { [bool]$schedule['enabled'] } else { $false }
        if (-not $isEnabled) {
            continue
        }

        foreach ($environment in @($schedule['environments'] | Sort-Object)) {
            $matrix += @{ environment = [string]$environment; secret_name = [string]$secretName }
        }
    }

    if ($matrix.Count -eq 0) {
        # GitHub still expects valid JSON output even when the workflow has nothing to do.
        $matrix = @(@{ environment = '__none__'; secret_name = '__none__'; skip = $true })
        Add-GitHubOutputValue -Name 'has_targets' -Value 'false'
    }
    else {
        Add-GitHubOutputValue -Name 'has_targets' -Value 'true'
    }

    Add-GitHubOutputValue -Name 'matrix' -Value ($matrix | ConvertTo-Json -Compress -AsArray)
}

function Get-RotationContext {
    Get-EnvironmentValue -Name 'TARGET_SECRET_NAME' -Required | Out-Null
    Get-EnvironmentValue -Name 'TARGET_ENVIRONMENT' -Required | Out-Null
    Get-EnvironmentValue -Name 'SUBSCRIPTION_PREFIX' -Required | Out-Null
    Get-EnvironmentValue -Name 'ENVIRONMENT_ID' -Required | Out-Null
    Get-EnvironmentValue -Name 'REGION_SHORT' -Required | Out-Null
    Get-EnvironmentValue -Name 'DESCRIPTOR' -Required | Out-Null
    Get-EnvironmentValue -Name 'GITHUB_OUTPUT' -Required | Out-Null

    $manifest = Get-Manifest
    $secrets = $manifest['secrets']
    $profiles = if ($manifest.ContainsKey('profiles')) { $manifest['profiles'] } else { @{} }
    $secretName = $env:TARGET_SECRET_NAME

    if (-not $secrets.ContainsKey($secretName)) {
        throw "Unsupported secret name: $secretName"
    }

    $secretDefinition = $secrets[$secretName]
    $profileName = if ($secretDefinition.ContainsKey('profile')) { [string]$secretDefinition['profile'] } else { '' }
    $profileDefinition = @{}

    if (-not [string]::IsNullOrWhiteSpace($profileName)) {
        if (-not $profiles.ContainsKey($profileName)) {
            throw "Secret '$secretName' references unknown profile '$profileName'."
        }
        $profileDefinition = $profiles[$profileName]
    }

    # Profiles hold shared defaults for an app/domain. Secret entries then override
    # only the pieces that differ, which keeps the manifest small as support grows.
    $terraformDefinition = Join-Hashtable -Base $(if ($profileDefinition.ContainsKey('terraform')) { $profileDefinition['terraform'] } else { @{} }) -Override $(if ($secretDefinition.ContainsKey('terraform')) { $secretDefinition['terraform'] } else { @{} })
    $resourceDefinition = Join-Hashtable -Base $(if ($profileDefinition.ContainsKey('resources')) { $profileDefinition['resources'] } else { @{} }) -Override $(if ($secretDefinition.ContainsKey('resources')) { $secretDefinition['resources'] } else { @{} })
    $verificationDefinition = Join-Hashtable -Base $(if ($profileDefinition.ContainsKey('verification')) { $profileDefinition['verification'] } else { @{} }) -Override $(if ($secretDefinition.ContainsKey('verification')) { $secretDefinition['verification'] } else { @{} })
    $scheduleDefinition = if ($secretDefinition.ContainsKey('schedule')) { $secretDefinition['schedule'] } else { @{} }

    $templateContext = @{
        environment = $env:TARGET_ENVIRONMENT
        subscription_prefix = $env:SUBSCRIPTION_PREFIX
        environment_id = $env:ENVIRONMENT_ID
        region_short = $env:REGION_SHORT
        descriptor = $env:DESCRIPTOR
    }

    $functionAppName = ''
    if ($resourceDefinition.ContainsKey('function_app_name')) {
        $functionAppName = Format-Template -Template ([string]$resourceDefinition['function_app_name']) -Context $templateContext
    }
    $templateContext['function_app_name'] = $functionAppName

    $scheduledEnabled = if ($scheduleDefinition.ContainsKey('enabled')) { [bool]$scheduleDefinition['enabled'] } else { $false }

    $outputs = @{
        tf_root = [string]$terraformDefinition['root']
        state_key = Format-Template -Template ([string]$terraformDefinition['state_key']) -Context $templateContext
        secret_name = $secretName
        secret_display_name = if ($secretDefinition.ContainsKey('display_name')) { [string]$secretDefinition['display_name'] } else { $secretName }
        resource_group_name = if ($resourceDefinition.ContainsKey('resource_group_name')) { Format-Template -Template ([string]$resourceDefinition['resource_group_name']) -Context $templateContext } else { '' }
        key_vault_name = if ($resourceDefinition.ContainsKey('key_vault_name')) { Format-Template -Template ([string]$resourceDefinition['key_vault_name']) -Context $templateContext } else { '' }
        function_app_name = $functionAppName
        base_url = if ($resourceDefinition.ContainsKey('base_url')) { Format-Template -Template ([string]$resourceDefinition['base_url']) -Context $templateContext } else { '' }
        scheduled_enabled = $scheduledEnabled.ToString().ToLowerInvariant()
        refresh_mode = if ($secretDefinition.ContainsKey('refresh_strategy')) { [string]$secretDefinition['refresh_strategy'] } elseif ($profileDefinition.ContainsKey('refresh_strategy')) { [string]$profileDefinition['refresh_strategy'] } else { '' }
        restart_mode = if ($secretDefinition.ContainsKey('restart_strategy')) { [string]$secretDefinition['restart_strategy'] } elseif ($profileDefinition.ContainsKey('restart_strategy')) { [string]$profileDefinition['restart_strategy'] } else { '' }
        verification_runner = if ($verificationDefinition.ContainsKey('runner')) { [string]$verificationDefinition['runner'] } else { '' }
        verification_project = if ($verificationDefinition.ContainsKey('project')) { [string]$verificationDefinition['project'] } else { '' }
        verification_filter = if ($verificationDefinition.ContainsKey('filter')) { [string]$verificationDefinition['filter'] } else { '' }
        verification_secret_env_name = if ($verificationDefinition.ContainsKey('secret_env_name')) { [string]$verificationDefinition['secret_env_name'] } else { '' }
        verification_previous_secret_env_name = if ($verificationDefinition.ContainsKey('previous_secret_env_name')) { [string]$verificationDefinition['previous_secret_env_name'] } else { '' }
    }

    foreach ($name in $outputs.Keys) {
        Add-GitHubOutputValue -Name $name -Value $outputs[$name]
    }

    Add-GitHubOutputMultiline -Name 'rotate_targets' -Values @($terraformDefinition['rotate_targets'])
    Add-GitHubOutputMultiline -Name 'replace_targets' -Values @($terraformDefinition['replace_targets'])
}

function Get-SecretMetadata {
    Get-EnvironmentValue -Name 'KEY_VAULT_NAME' -Required | Out-Null
    Get-EnvironmentValue -Name 'SECRET_NAME' -Required | Out-Null
    Get-EnvironmentValue -Name 'ROTATION_WINDOW_DAYS' -Required | Out-Null
    Get-EnvironmentValue -Name 'GITHUB_EVENT_NAME' -Required | Out-Null
    Get-EnvironmentValue -Name 'GITHUB_OUTPUT' -Required | Out-Null

    $scheduledEnabled = [System.Convert]::ToBoolean((Get-EnvironmentValue -Name 'SCHEDULED_ENABLED' -Default 'false'))
    $secretJson = & az keyvault secret show --vault-name $env:KEY_VAULT_NAME --name $env:SECRET_NAME -o json | ConvertFrom-Json -Depth 16
    $secretId = [string]$secretJson.id
    $secretVersion = ($secretId -split '/')[-1]
    $secretExpires = [string]$secretJson.attributes.expires

    if ([string]::IsNullOrWhiteSpace($secretExpires)) {
        throw "Secret expiry metadata is missing for $($env:SECRET_NAME)."
    }

    $now = [DateTimeOffset]::UtcNow
    $expiry = [DateTimeOffset]::Parse($secretExpires).ToUniversalTime()
    $daysRemaining = [math]::Floor(($expiry - $now).TotalDays)

    $shouldRotate = 'false'
    if ($env:GITHUB_EVENT_NAME -eq 'workflow_dispatch') {
        $shouldRotate = 'true'
    }
    elseif ($scheduledEnabled -and $daysRemaining -le [int](Get-EnvironmentValue -Name 'ROTATION_WINDOW_DAYS' -Required)) {
        $shouldRotate = 'true'
    }

    Add-GitHubOutputValue -Name 'version' -Value $secretVersion
    Add-GitHubOutputValue -Name 'expires' -Value $secretExpires
    Add-GitHubOutputValue -Name 'days_remaining' -Value ([string]$daysRemaining)
    Add-GitHubOutputValue -Name 'should_rotate' -Value $shouldRotate
}

function Invoke-RotationPlan {
    Get-EnvironmentValue -Name 'TF_ROOT' -Required | Out-Null
    Get-EnvironmentValue -Name 'TFVARS_PATH' -Required | Out-Null
    Get-EnvironmentValue -Name 'PLAN_FILE' -Required | Out-Null

    $arguments = @(
        "-input=false"
        "-no-color"
        "-var-file=../$($env:TFVARS_PATH)"
        "-out=$($env:PLAN_FILE)"
    )

    foreach ($target in (Get-MultilineEnvItems -Name 'ROTATE_TARGETS')) {
        $arguments += "-target=$target"
    }

    foreach ($replacement in (Get-MultilineEnvItems -Name 'REPLACE_TARGETS')) {
        $arguments += "-replace=$replacement"
    }

    & terraform "-chdir=$($env:TF_ROOT)" plan @arguments
}

function Update-DependentConfig {
    $refreshMode = Get-EnvironmentValue -Name 'REFRESH_MODE'
    $restartMode = Get-EnvironmentValue -Name 'RESTART_MODE'
    $targetSecretName = Get-EnvironmentValue -Name 'TARGET_SECRET_NAME' -Default 'unknown'

    # Refresh/restart behavior is strategy-driven so future secret types can reuse
    # the same workflow shape without hardcoding app-specific branches into YAML.
    switch ($refreshMode) {
        'function_app_key_vault_reference' {
            Get-EnvironmentValue -Name 'FUNCTION_APP_NAME' -Required | Out-Null
            Get-EnvironmentValue -Name 'RESOURCE_GROUP_NAME' -Required | Out-Null

            $functionAppId = & az functionapp show `
                --name $env:FUNCTION_APP_NAME `
                --resource-group $env:RESOURCE_GROUP_NAME `
                --query id `
                -o tsv

            & az rest `
                --method post `
                --uri "https://management.azure.com$functionAppId/config/configreferences/appsettings/refresh?api-version=2022-03-01" `
                | Out-Null
        }
        '' {
            Write-Host "No app configuration refresh required for $targetSecretName."
        }
        default {
            throw "Unsupported refresh mode: $refreshMode"
        }
    }

    switch ($restartMode) {
        'function_app' {
            Get-EnvironmentValue -Name 'FUNCTION_APP_NAME' -Required | Out-Null
            Get-EnvironmentValue -Name 'RESOURCE_GROUP_NAME' -Required | Out-Null

            & az functionapp restart `
                --name $env:FUNCTION_APP_NAME `
                --resource-group $env:RESOURCE_GROUP_NAME
        }
        '' {
            Write-Host "No app restart required for $targetSecretName."
        }
        default {
            throw "Unsupported restart mode: $restartMode"
        }
    }
}

function Set-RotationSummary {
    Get-EnvironmentValue -Name 'GITHUB_STEP_SUMMARY' -Required | Out-Null

    $jobStatus = Get-EnvironmentValue -Name 'JOB_STATUS' -Default 'unknown'
    $mode = if ((Get-EnvironmentValue -Name 'APPLY_ROTATION' -Default 'false') -eq 'true') { 'apply' } else { 'plan' }
    $statusIcon = if ($jobStatus -eq 'success') { '✅' } else { '❌' }
    $shouldRotate = Get-EnvironmentValue -Name 'SHOULD_ROTATE' -Default 'false'
    $functionAppName = Get-EnvironmentValue -Name 'FUNCTION_APP_NAME'

    $lines = @(
        "## $statusIcon Key Vault secret rotation"
        ''
        "- **Environment:** ``$(Get-EnvironmentValue -Name 'TARGET_ENVIRONMENT' -Default 'unknown')``"
        "- **Secret name:** ``$(Get-EnvironmentValue -Name 'TARGET_SECRET_NAME' -Default 'unknown')``"
        "- **Reason:** ``$(Get-EnvironmentValue -Name 'ROTATION_REASON' -Default 'unknown')``"
        "- **Mode:** ``$mode``"
        "- **Display name:** ``$(Get-EnvironmentValue -Name 'SECRET_DISPLAY_NAME' -Default 'unknown')``"
        "- **Current version before run:** ``$(Get-EnvironmentValue -Name 'CURRENT_VERSION_BEFORE' -Default 'unknown')``"
        "- **Current expiry before run:** ``$(Get-EnvironmentValue -Name 'CURRENT_EXPIRY_BEFORE' -Default 'unknown')``"
        "- **Days remaining before run:** ``$(Get-EnvironmentValue -Name 'DAYS_REMAINING_BEFORE' -Default 'unknown')``"
        "- **Rotation required:** ``$shouldRotate``"
    )

    if (-not [string]::IsNullOrWhiteSpace($functionAppName)) {
        $lines += "- **Function app:** ``$functionAppName``"
    }

    if ($shouldRotate -eq 'false') {
        $lines += '- **Scheduled action:** skipped'
    }

    $currentVersionAfter = Get-EnvironmentValue -Name 'CURRENT_VERSION_AFTER'
    if (-not [string]::IsNullOrWhiteSpace($currentVersionAfter)) {
        $lines += "- **Current version after rotation:** ``$currentVersionAfter``"
    }

    $currentExpiryAfter = Get-EnvironmentValue -Name 'CURRENT_EXPIRY_AFTER'
    if (-not [string]::IsNullOrWhiteSpace($currentExpiryAfter)) {
        $lines += "- **Current expiry after rotation:** ``$currentExpiryAfter``"
    }

    $lines += "- **Job status:** $statusIcon ``$jobStatus``"
    $lines += ''
    $lines += '> This workflow performs a single-active cutover for supported secrets. Multi-secret overlap is tracked separately.'

    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $lines
}

# Keep the public command surface small and workflow-oriented. New behavior should
# usually extend one of these phases rather than adding lots of one-off commands.
switch ($Command) {
    'matrix' { Get-RotationMatrix }
    'context' { Get-RotationContext }
    'inspect' { Get-SecretMetadata }
    'plan' { Invoke-RotationPlan }
    'apply-config' { Update-DependentConfig }
    'summary' { Set-RotationSummary }
    default { throw "Unsupported command: $Command" }
}
