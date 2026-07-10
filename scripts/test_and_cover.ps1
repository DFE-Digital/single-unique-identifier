param(
    [Parameter(Mandatory=$true)]
    [string]$SolutionPath
)

$ErrorActionPreference = "Stop"

# Ensure script resolves the actual repository root (one level up from /scripts)
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = Resolve-Path "$ScriptRoot\.." | Select-Object -ExpandProperty Path

# Emulate the old behavior by starting in the repo root
Push-Location $RepoRoot

Write-Host "Running script from: $RepoRoot"
Write-Host "Using solution path: $SolutionPath"

# Determine solution folder and file name
$solutionDir = Split-Path $SolutionPath -Parent
$solutionFileName = Split-Path $SolutionPath -Leaf

# Switch to solution folder so dotnet commands work correctly
Push-Location $solutionDir

# Directories for coverage reports (Pointing to the repo root)
$resultsDir = "$RepoRoot/coverage"
$finalReportDir = "$resultsDir/coveragereport"

# Remove old coverage folder
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

# Remove all TestResults folders
Get-ChildItem -Path . -Recurse -Directory -Filter "TestResults" | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
}

# Build solution
dotnet clean
dotnet build --no-incremental

# Run tests
$runSettingsFile = "tests.runsettings"

dotnet test $solutionFileName `
    --no-build `
    --verbosity minimal `
    --collect:"XPlat Code Coverage" `
    --settings $runSettingsFile

# Generate coverage report
dotnet tool restore

dotnet tool run reportgenerator `
    -reports:"**/TestResults/*/coverage.cobertura.xml" `
    -targetdir:$finalReportDir `
    -reporttypes:SonarQube,html

# Open HTML report
if (Test-Path "$finalReportDir/index.html") {
    Invoke-Item -Path "$finalReportDir/index.html"
} else {
    Write-Warning "Coverage report not generated. Check test run for errors."
}

# Cleanup
Pop-Location # back to solution folder
Pop-Location # back to original directory
