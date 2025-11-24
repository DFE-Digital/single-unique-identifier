param(
    [Parameter(Mandatory=$true)]
    [string]$SolutionPath
)

$ErrorActionPreference = "Stop"

# Determine solution folder and file name
$solutionDir = Split-Path $SolutionPath -Parent
$solutionFileName = Split-Path $SolutionPath -Leaf

# Change to solution folder to ensure dotnet commands work
Push-Location $solutionDir

# Directories for coverage reports
$resultsDir = "./coverage"
$finalReportDir = "$resultsDir/coveragereport"

# Clean old coverage folder
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

# Remove all TestResults folders to avoid stale results
Get-ChildItem -Path . -Recurse -Directory -Filter "TestResults" | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
}

# Clean & build solution
dotnet clean
dotnet build --no-incremental

# Use runsettings in the current folder (fixes doubled path issue)
$runSettingsFile = "tests.runsettings"

# Run tests with XPlat Code Coverage
dotnet test $solutionFileName --no-build --verbosity minimal --collect:"XPlat Code Coverage" --settings $runSettingsFile

# Restore and run local reportgenerator tool
dotnet tool restore
dotnet tool run reportgenerator -reports:"**/TestResults/*/coverage.cobertura.xml" -targetdir:$finalReportDir -reporttypes:SonarQube,html

# Open HTML report in default browser if it exists
if (Test-Path "$finalReportDir/index.html") {
    open "$finalReportDir/index.html"
} else {
    Write-Warning "Coverage report not generated. Check test run for errors."
}

# Return to original directory
Pop-Location
