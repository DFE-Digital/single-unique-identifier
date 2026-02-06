# Single Unique Identifier

This repository is a mono repo that contains multiple .NET solutions, each organised under its own directory.
.NET solutions follow the [Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture) principle ensuring seperation of concerns, maintainability and testability.

To view our documentation, please visit the [Docs](/Docs/index.md) directory.

| Directory/File                   | Description                                                                                                 |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| [Apps](/Apps)                    | The Apps and Components created for the single unique identifier programme.                                 |
| [Docs](/Docs)                    | Programme technical documentation, including architecture models and decisions.                             |
| [SystemTests](/SystemTests/)     | .NET solution, providing Gherkin feature definitions of functional requirements covering the entire system. |
| [LICENCE](/LICENCE)              | Standard DfE software licence <!-- Yes, that is spelled correctly. -->, applying to the entire system.      |
| [Contributing](/CONTRIBUTING.md) | Contributions guide for this repisitory. Please read before contributing                                    |

## What is 'Single Unique Identifier'?

Single Unique Identifier is a proposed set of systems and standards to faciliate information
sharing between multiple agencies for the improved safeguarding and welfare of children.

## Glossary of Components

* **`Matching Service`** (a.k.a. *`PDS Adapter`* or *`SUI.Matching.API`*) \
  Match a PDS record (and NHS Number), given some demographic data.

* **`MatchFunction`** (*"I know of this person, what is their ID"*) \
  Enables Custodians to tell us that they have a record.
  Invokes Matching Service (above), and updates the ID register; given some demographic data and some metadata about the data (record) they hold.

* **`SearchFunction`** (a.k.a. *`Find-a-record`*) \
  Find record pointers for a given SUI.

* **`FetchRecordFunction`** (a.k.a. *`Fetch-a-record`* or *`Fetch`*) \
  Resolve and return the actual data for a given record pointer.

* **`StubCustodians`** (a.k.a. *`Stubs`*) \
  Stub API that simulates real Custodians, to provide example data to Find and Fetch for testing purposes.

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) version 9.0.300 or later

### Setup

To set up the development environment, restore the required .NET tools:

```bash
dotnet tool restore
dotnet husky install
```

This will install the tools specified in `.config/dotnet-tools.json`, including CSharpier for code formatting and Husky.Net for pre-commit hooks to ensure consistent code style.

### Local OpenTelemetry

To view local traces and logs from any app, run the Grafana otel-lgtm stack:

```bash
docker run --rm -p 3000:3000 -p 4317:4317 -p 4318:4318 -ti --name sui-grafana grafana/otel-lgtm
```

Alternatively, you can use Aspire Dashboard to view traces and logs:

```bash
docker run --rm -p 18888:18888 -p 4317:18889 -ti --name sui-aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest --env ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
```

Point your app at the local collector by specifying the values in the `local.settings.json` file:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_LOGS_EXPORTER=otlp
OTEL_TRACES_SAMPLER=always_on
OTEL_SERVICE_NAME=Your.App.Name
```

Open `http://localhost:3000` (admin/admin) and use Explore to view logs and traces.
If using Aspire Dashboard, navigate to `http://localhost:18888`.

## Repository structure

Each solution is self-contained and follows a consistant structure:

```
Apps/AppOrComponentName/
  src/
    YourCsProjects/
  tests/
    YourCsTestProject.Unit.Tests/
    YourCsTestProject.Integration.Tests/
```

## Clean architecture

Each solution is structured according to Clean Architecture principles

- Domain - Core business logic and entities.
- Application - Application logic
- Infrastructure - External concerns (e.g. database, third party API calls)
- Presentation/API - UI, API/Endpoints

## Run tests and collect coverage locally

You can run tests and generate coverage reports from anywhere within the repository by following the steps below.
All required tools (PowerShell 7.5.4, ReportGenerator, etc.) are installed as local .NET tools.

1. Restore local tools (This installs PowerShell and all other required tools locally.)

```
dotnet tool restore
```

2. Run the test & coverage scripts (You can run the wrapper script for each solution i.e: `dotnet pwsh <relative path to the solution scoped script>`)

```
dotnet pwsh ./Apps/Transfer/test_and_cover_transfer.ps1
dotnet pwsh ./Apps/SingleView/test_and_cover_singleview.ps1
dotnet pwsh ./Apps/StubCustodians/test_and_cover_stubcustodians.ps1
dotnet pwsh ./Apps/Find/test_and_cover_find.ps1
dotnet pwsh ./Apps/Matching/test_and_cover_matching.ps1
```

## Set up Private Nuget Feed

We are using GitHub Packages for hosting API Clients, this feed require you to sign in to be able to do a restore.
You will need to get a 'Personal Access Token (Classic)' from GitHub.

click on your photo in the top right > click Settings > Developer Settings > Personal access tokens > Tokens (classic) >
click generate new token (classic) > add a note to describe the token > set an expiration date >
check the 'write:packages' permission > click Generate token.

Save a copy on the token on your machine incase you need it in the future.

### JetBrains Rider IDE

When you try and restore a project for the first time that is using a GitHub package feed, Rider will bring up a login box.
Put your GitHub username in the username field and your Token in the password field.

### CLI

If using the `dotnet` cli tool, you can set the `GITHUB_USERNAME` and `GITHUB_TOKEN_DFENUGET` environment variables (referenced in the `nuget.config` files) to specify the credentials for the nuget feed.

Example:

```
GITHUB_USERNAME=YourGitHubUsername GITHUB_TOKEN_DFENUGET=YourTokenHere dotnet watch run --launch-profile https
```
