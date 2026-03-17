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

function Get-RequiredEnvironmentValue {
    param([string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$Name is required."
    }

    return $value
}

function Test-RequiredFilePath {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
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

function Get-EnvOrDefault {
    param(
        [string]$Name,
        [string]$Default = ''
    )

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrEmpty($value)) {
        return $Default
    }

    return $value
}

function Get-MultilineEnvItems {
    param([string]$Name)

    $value = Get-EnvOrDefault -Name $Name
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

function ConvertTo-Hashtable {
    param($InputObject)

    if ($null -eq $InputObject) {
        return @{}
    }

    if ($InputObject -is [hashtable]) {
        $result = @{}
        foreach ($key in $InputObject.Keys) {
            $result[$key] = ConvertTo-Hashtable -InputObject $InputObject[$key]
        }
        return $result
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $result = @{}
        foreach ($key in $InputObject.Keys) {
            $result[[string]$key] = ConvertTo-Hashtable -InputObject $InputObject[$key]
        }
        return $result
    }

    if ($InputObject -is [System.Collections.IEnumerable] -and -not ($InputObject -is [string])) {
        $items = @()
        foreach ($item in $InputObject) {
            $items += ,(ConvertTo-Hashtable -InputObject $item)
        }
        return $items
    }

    if ($InputObject -is [psobject]) {
        $properties = $InputObject.PSObject.Properties
        if ($properties.Count -gt 0) {
            $result = @{}
            foreach ($property in $properties) {
                $result[$property.Name] = ConvertTo-Hashtable -InputObject $property.Value
            }
            return $result
        }
    }

    return $InputObject
}

function Get-Manifest {
    Test-RequiredFilePath -Path $manifestPath
    return ConvertTo-Hashtable -InputObject (Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -Depth 32)
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
    Get-RequiredEnvironmentValue -Name 'GITHUB_EVENT_NAME' | Out-Null
    Get-RequiredEnvironmentValue -Name 'GITHUB_OUTPUT' | Out-Null

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
        $matrix = @(@{ environment = '__none__'; secret_name = '__none__'; skip = $true })
        Add-GitHubOutputValue -Name 'has_targets' -Value 'false'
    }
    else {
        Add-GitHubOutputValue -Name 'has_targets' -Value 'true'
    }

    Add-GitHubOutputValue -Name 'matrix' -Value ($matrix | ConvertTo-Json -Compress -AsArray)
}

function Get-RotationContext {
    Get-RequiredEnvironmentValue -Name 'TARGET_SECRET_NAME' | Out-Null
    Get-RequiredEnvironmentValue -Name 'TARGET_ENVIRONMENT' | Out-Null
    Get-RequiredEnvironmentValue -Name 'SUBSCRIPTION_PREFIX' | Out-Null
    Get-RequiredEnvironmentValue -Name 'ENVIRONMENT_ID' | Out-Null
    Get-RequiredEnvironmentValue -Name 'REGION_SHORT' | Out-Null
    Get-RequiredEnvironmentValue -Name 'DESCRIPTOR' | Out-Null
    Get-RequiredEnvironmentValue -Name 'GITHUB_OUTPUT' | Out-Null

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
    }

    foreach ($name in $outputs.Keys) {
        Add-GitHubOutputValue -Name $name -Value $outputs[$name]
    }

    Add-GitHubOutputMultiline -Name 'rotate_targets' -Values @($terraformDefinition['rotate_targets'])
    Add-GitHubOutputMultiline -Name 'replace_targets' -Values @($terraformDefinition['replace_targets'])
}

function Get-SecretMetadata {
    Get-RequiredEnvironmentValue -Name 'KEY_VAULT_NAME' | Out-Null
    Get-RequiredEnvironmentValue -Name 'SECRET_NAME' | Out-Null
    Get-RequiredEnvironmentValue -Name 'ROTATION_WINDOW_DAYS' | Out-Null
    Get-RequiredEnvironmentValue -Name 'GITHUB_EVENT_NAME' | Out-Null
    Get-RequiredEnvironmentValue -Name 'GITHUB_OUTPUT' | Out-Null

    $scheduledEnabled = [System.Convert]::ToBoolean((Get-EnvOrDefault -Name 'SCHEDULED_ENABLED' -Default 'false'))
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
    elseif ($scheduledEnabled -and $daysRemaining -le [int](Get-RequiredEnvironmentValue -Name 'ROTATION_WINDOW_DAYS')) {
        $shouldRotate = 'true'
    }

    Add-GitHubOutputValue -Name 'version' -Value $secretVersion
    Add-GitHubOutputValue -Name 'expires' -Value $secretExpires
    Add-GitHubOutputValue -Name 'days_remaining' -Value ([string]$daysRemaining)
    Add-GitHubOutputValue -Name 'should_rotate' -Value $shouldRotate
}

function Invoke-RotationPlan {
    Get-RequiredEnvironmentValue -Name 'TF_ROOT' | Out-Null
    Get-RequiredEnvironmentValue -Name 'TFVARS_PATH' | Out-Null
    Get-RequiredEnvironmentValue -Name 'PLAN_FILE' | Out-Null

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
    $refreshMode = Get-EnvOrDefault -Name 'REFRESH_MODE'
    $restartMode = Get-EnvOrDefault -Name 'RESTART_MODE'
    $targetSecretName = Get-EnvOrDefault -Name 'TARGET_SECRET_NAME' -Default 'unknown'

    switch ($refreshMode) {
        'function_app_key_vault_reference' {
            Get-RequiredEnvironmentValue -Name 'FUNCTION_APP_NAME' | Out-Null
            Get-RequiredEnvironmentValue -Name 'RESOURCE_GROUP_NAME' | Out-Null

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
            Get-RequiredEnvironmentValue -Name 'FUNCTION_APP_NAME' | Out-Null
            Get-RequiredEnvironmentValue -Name 'RESOURCE_GROUP_NAME' | Out-Null

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
    Get-RequiredEnvironmentValue -Name 'GITHUB_STEP_SUMMARY' | Out-Null

    $jobStatus = Get-EnvOrDefault -Name 'JOB_STATUS' -Default 'unknown'
    $mode = if ((Get-EnvOrDefault -Name 'APPLY_ROTATION' -Default 'false') -eq 'true') { 'apply' } else { 'plan' }
    $statusIcon = if ($jobStatus -eq 'success') { '✅' } else { '❌' }
    $shouldRotate = Get-EnvOrDefault -Name 'SHOULD_ROTATE' -Default 'false'
    $functionAppName = Get-EnvOrDefault -Name 'FUNCTION_APP_NAME'

    $lines = @(
        "## $statusIcon Key Vault secret rotation"
        ''
        "- **Environment:** ``$(Get-EnvOrDefault -Name 'TARGET_ENVIRONMENT' -Default 'unknown')``"
        "- **Secret name:** ``$(Get-EnvOrDefault -Name 'TARGET_SECRET_NAME' -Default 'unknown')``"
        "- **Reason:** ``$(Get-EnvOrDefault -Name 'ROTATION_REASON' -Default 'unknown')``"
        "- **Mode:** ``$mode``"
        "- **Display name:** ``$(Get-EnvOrDefault -Name 'SECRET_DISPLAY_NAME' -Default 'unknown')``"
        "- **Current version before run:** ``$(Get-EnvOrDefault -Name 'CURRENT_VERSION_BEFORE' -Default 'unknown')``"
        "- **Current expiry before run:** ``$(Get-EnvOrDefault -Name 'CURRENT_EXPIRY_BEFORE' -Default 'unknown')``"
        "- **Days remaining before run:** ``$(Get-EnvOrDefault -Name 'DAYS_REMAINING_BEFORE' -Default 'unknown')``"
        "- **Rotation required:** ``$shouldRotate``"
    )

    if (-not [string]::IsNullOrWhiteSpace($functionAppName)) {
        $lines += "- **Function app:** ``$functionAppName``"
    }

    if ($shouldRotate -eq 'false') {
        $lines += '- **Scheduled action:** skipped'
    }

    $currentVersionAfter = Get-EnvOrDefault -Name 'CURRENT_VERSION_AFTER'
    if (-not [string]::IsNullOrWhiteSpace($currentVersionAfter)) {
        $lines += "- **Current version after rotation:** ``$currentVersionAfter``"
    }

    $currentExpiryAfter = Get-EnvOrDefault -Name 'CURRENT_EXPIRY_AFTER'
    if (-not [string]::IsNullOrWhiteSpace($currentExpiryAfter)) {
        $lines += "- **Current expiry after rotation:** ``$currentExpiryAfter``"
    }

    $lines += "- **Job status:** $statusIcon ``$jobStatus``"
    $lines += ''
    $lines += '> This workflow performs a single-active cutover for supported secrets. Multi-secret overlap is tracked separately.'

    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $lines
}

switch ($Command) {
    'matrix' { Get-RotationMatrix }
    'context' { Get-RotationContext }
    'inspect' { Get-SecretMetadata }
    'plan' { Invoke-RotationPlan }
    'apply-config' { Update-DependentConfig }
    'summary' { Set-RotationSummary }
    default { throw "Unsupported command: $Command" }
}
