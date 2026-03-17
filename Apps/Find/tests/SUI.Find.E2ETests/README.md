# E2E testing

## What this is for

E2E tests are designed to be as light as possible and to check as little as possible. Leave fine details to Unit/Integration testing.
In this project we are testing the Happy path to ensure all components are functioning correctly.

## How to run it locally

Because azure dotnet isolated function do not play well in memory, this 
requires you to test against running applications.

1. Ideally, clean out your Azurite storage tables for a clean run.
2. Start the SUI.Find.FindApi and SUI.StubCustodians.
   - JetBrains Rider can throw up runtime issues with durable functions so it's recommended to use the function app CLI 'func'
   - To run in CLI:
     - For SUI.Find.FindApi, go into project directory and run `func start --port 7182`
     - For SUI.StubCustodians, go into project directory and run `dotnet run`
     - Run these in different terminal windows so they can run simultaneously and view the logs.
3. Run the E2E tests in CLI or an IDE of your choice.
4. Stop the SUI.Find.FindApi and SUI.StubCustodians.

## How to run it against the dev environment

To run the E2E tests against the dev environment, from your local machine:

```
dotnet test -e E2E__BaseUrl=https://s270d01func-ukw-find01.azurewebsites.net/api/ -e E2E__StubCustodiansBaseUrl=https://s270d01app-ukw-custodians01.azurewebsites.net/api/ -e E2E__SkipResetAzureTables=True
```

To run just the smoke-test subset used by the secret-rotation workflow:

```
dotnet test --filter "Category=Smoke" -e E2E__BaseUrl=https://s270d01func-ukw-find01.azurewebsites.net/api/ -e E2E__FindApiKey="{FindApiKey}"
```

Replacing `{FindApiKey}` with the current Find Match API key value for the target environment.

To run the E2E tests against the dev environment, with a clean execution state, from your local machine:
```
dotnet test -e E2E__BaseUrl=https://s270d01func-ukw-find01.azurewebsites.net/api/ -e E2E__StubCustodiansBaseUrl=https://s270d01app-ukw-custodians01.azurewebsites.net/api/ -e E2E__FindApiStorageConnectionString="{StorageConnStr}"
```

Replacing `{StorageConnStr}` with the FindAPI's storage connection string for the dev environment. Example format: `DefaultEndpointsProtocol=https;AccountName={AccountName};AccountKey={AccountName};EndpointSuffix=core.windows.net`.  This may however cause 'Conflict... TableBeingDeleted' errors.
