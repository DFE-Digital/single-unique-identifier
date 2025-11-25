param(
    [Parameter(Mandatory=$true)]
    [string]$SolutionPath
)

$ErrorActionPreference = "Stop"

# Ensure script runs from the folder the script is in
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
Push-Location $ScriptRoot

Write-Host "Running script from: $ScriptRoot"
Write-Host "Using solution path: $SolutionPath"

# Determine solution folder and file name
$solutionDir = Split-Path $SolutionPath -Parent
$solutionFileName = Split-Path $SolutionPath -Leaf

# Switch to solution folder so dotnet commands work correctly
Push-Location $solutionDir

# Directories for coverage reports
$resultsDir = "$ScriptRoot/coverage"
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
    open "$finalReportDir/index.html"
} else {
    Write-Warning "Coverage report not generated. Check test run for errors."
}

# Cleanup
Pop-Location # back to solution folder
Pop-Location # back to original directory
