docker compose --profile aspire down
docker compose --profile grafana down
docker compose --profile aspire up -d

# Build
echo "Building .NET projects..."
$procs = $(
    Start-Process dotnet "build" -NoNewWindow -WorkingDirectory Apps/StubCustodians/src/SUI.StubCustodians.API -PassThru;
    Start-Process dotnet "build" -NoNewWindow -WorkingDirectory Apps/Find/src/SUI.Find.FindApi -PassThru;
)
$procs | Wait-Process
echo "Built .NET projects ok"

# Start apps
echo "Starting apps..."
$procs = $(
    Start-Process func "start --port 7182" -NoNewWindow -WorkingDirectory Apps/Find/src/SUI.Find.FindApi -PassThru;
    Start-Process dotnet "run --no-build" -NoNewWindow -WorkingDirectory Apps/StubCustodians/src/SUI.StubCustodians.API -PassThru;
)

# Run e2e tests
echo "Running e2e tests..."
dotnet test Apps/Find/tests/SUI.Find.E2ETests/SUI.Find.E2ETests.csproj `
    --filter "Category=E2E&Suite=Standard" `
    --logger "console;verbosity=detailed" `
    --logger "trx;LogFileName=e2e-test-results.trx" `
    --results-directory TestResults

# Stop apps
echo "Stopping apps..."
Stop-Process $procs
$procs | Wait-Process

echo "Done"
