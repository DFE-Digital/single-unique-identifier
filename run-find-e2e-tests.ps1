<#
    .DESCRIPTION
    Rudimentary script for reliably running e2e tests locally

    .EXAMPLE
    dotnet pwsh ./run-find-e2e-tests.ps1
        (Having already run `dotnet tool restore` as a one-off prerequisite. Using the dotnet tool alleviates PowerShell execution policy issues.)
#>

$ErrorActionPreference = "Stop"

function SetupAndRunTests {
    docker compose --profile aspire down
    docker compose --profile grafana down
    docker compose --profile aspire up -d

    $procs = $null
    try
    {
        # Build
        Write-Host "Building .NET projects..."
        $procs = $(
            Start-Process dotnet "build" -NoNewWindow -WorkingDirectory Apps/Find/src/SUI.Find.FindApi -PassThru;
            Start-Process dotnet "build" -NoNewWindow -WorkingDirectory Apps/StubCustodians/src/SUI.StubCustodians.API -PassThru;
        )
        $procs | Wait-Process
        Write-Host "Built .NET projects ok"

        # Start apps
        Write-Host "Starting apps..."
        $procs = $(
            Start-Process func "start --port 7182" -NoNewWindow -WorkingDirectory Apps/Find/src/SUI.Find.FindApi -PassThru;
            Start-Process dotnet "run --no-build" -NoNewWindow -WorkingDirectory Apps/StubCustodians/src/SUI.StubCustodians.API -PassThru;
        )

        # Wait for Find API to be up...
        WaitForAPI -Url "http://localhost:7182/api/health" -Name "Find API"

        # Run e2e tests
        Write-Host "Running e2e tests..."
        dotnet test Apps/Find/tests/SUI.Find.E2ETests/SUI.Find.E2ETests.csproj `
            --filter "Category=E2E&Suite=Standard" `
            --logger "console;verbosity=detailed" `
            --logger "trx;LogFileName=e2e-test-results.trx" `
            --results-directory TestResults
    }
    finally
    {
        Write-Host "Stopping child processes..."
        Stop-Process $procs
        $procs | Wait-Process
    }

    Write-Host "Done"
}

function WaitForAPI {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [int]$TimeoutSeconds = 30,
        [int]$PollIntervalMs = 250,
        [string]$ExpectedValue = "Healthy"
    )

    Write-Host "Waiting for $Name $Url to be up..." -ForegroundColor Cyan

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $isHealthy = $false
    $healthCheckError = $null

    while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        try {
            $response = Invoke-RestMethod -Uri $Url -Method Get -ErrorAction Stop
            
            if ($response.Value -eq $ExpectedValue) {
                $isHealthy = $true
                Write-Host "$Name $Url is up (Took $($stopwatch.Elapsed.TotalSeconds.ToString('F2')) seconds)" -ForegroundColor Green
                break
            }
        }
        catch {
            Write-Host "$Name $Url does not yet appear to be up (timeout in $(($TimeoutSeconds - $stopwatch.Elapsed.TotalSeconds).ToString('F2')) seconds)..." -ForegroundColor Magenta
            Write-Verbose "Health check attempt failed: $_"
            $healthCheckError = $_
        }

        Start-Sleep -Milliseconds $PollIntervalMs
    }

    $stopwatch.Stop()

    if (-not $isHealthy) {
        throw "$Name $Url did not indicate healthy within $TimeoutSeconds seconds ($healthCheckError)"
    }
}

# Setup and run the tests...
SetupAndRunTests
