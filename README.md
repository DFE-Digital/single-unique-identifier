# Single Unique Identifier

This repository is a mono repo that contains multiple .NET solutions, each organised under its own directory.
The .NET solutions follow the [Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture) principle ensuring separation of concerns, maintainability and testability.

To view our technical documentation, please visit the [Docs](./Docs/index.md) directory.

Looking to getting started with local development? Skip to [Getting Started](#getting-started).

| Directory/File                    | Description                                                                                                 |
|-----------------------------------|-------------------------------------------------------------------------------------------------------------|
| [Apps](./Apps)                    | The Apps and Components created for the single unique identifier programme.                                 |
| [Docs](./Docs)                    | Programme technical documentation, including architecture models and decisions.                             |
| [SystemTests](./SystemTests)      | .NET solution, providing Gherkin feature definitions of functional requirements covering the entire system. |
| [LICENCE](./LICENCE)              | Standard DfE software licence<!-- Yes, that is spelled correctly. -->, applying to the entire system.       |
| [Contributing](./CONTRIBUTING.md) | Contributions guide for this repisitory. Please read before contributing.                                   |


## What is 'Single Unique Identifier'?

Single Unique Identifier is a proposed set of systems and standards to facilitate information
sharing between multiple agencies for the improved safeguarding and welfare of children.

Today, information about a child is distributed across many independent systems — in education, health, children’s social care, police, youth justice, early years and more. When these systems cannot communicate or reliably match records, important details can be missed, duplicated, or delayed.

This project investigates the technical foundations required to help practitioners access the right information at the right time, while maintaining strong standards of privacy, security, and data minimisation.

This project is in the Discovery Phase.  This means everything in this repository should be considered exploratory, not production‑ready.

### What This Discovery Phase Is Exploring

This Discovery phase is focused on learning, prototyping, and testing.  It does **not** create a final service — it explores what could work.

Key areas of exploration include:

1. Improving Identity Matching ("Match")
2. Finding Who Holds Information ("Find")
3. Understanding Future Data Exchange ("Fetch")

The Discovery phase aims to understand needs, test technical feasibility, explore architectural options, identify risks, engage system suppliers, produce evidence to inform future Alpha and Beta phases, and validate whether the approach could support a future national service.

### Security, Trust, and Privacy

A core principle of this work is that **safeguarding information must be protected**.  
The Discovery phase therefore emphasises:

- Data minimisation
- Clear audit logging
- Strong authentication and role‑based access
- Privacy‑by‑design throughout the architecture
- No central store of case data
- Only the minimum metadata required to support safe decision‑making

This project explores _how_ a safe, trusted, multi‑agency approach could be implemented — not just the technology, but the standards and safeguards required.


## Glossary of Components

### `MatchingService` (a.k.a. *`PDS Adapter`*)
* Match a PDS record and return the NHS Number, given some demographic information about a child.

### `MatchFunction` (a.k.a. *`Get-an-Identifier`*)
* A service that takes basic demographic information (name, date of birth, address) and determines the most accurate identifier for a child.  
* This service also enables agencies (custodians) to tell us that they have a record about the provided demographic information.  
* This helps link records across multiple organisations with higher confidence.  
* This service invokes the `MatchingService` (above), and updates the ID register. Only the SUI and custodian metadata is stored.

### `SearchFunction` (a.k.a. *`Find-a-record`*)
* A mechanism to identify which custodians have a record relating to a child, **without sharing any case data**.  
* This gives practitioners a starting point for collaboration.  
* Returns record pointers for a given SUI.

### `FetchRecordFunction` (a.k.a. *`Fetch-a-record`*)
* Resolves and returns the record for a given record pointer.  
* This may include a summary of the record, a link to a custodian system where authorised users can view the full details, or both.  
* Some custodians may provide contact details only.  
* This explores secure, controlled sharing of small agreed‑upon information packets between systems.  
* This is not part of Discovery, but Discovery lays the groundwork for understanding whether and how this could be feasible.

### `StubCustodians` (a.k.a. *`Stubs`*)
* Stub API that simulates real custodians, to provide example data for testing purposes.


## Record Types Reference

| C# Type Name                     | Record Type ID             | Schema URI                                                                |
|----------------------------------|----------------------------|---------------------------------------------------------------------------|
| `ChildrensServicesDetailsRecord` | childrens-services.details | https://schemas.example.gov.uk/sui/ChildrensServicesDetailsRecordV1.json  |
| `CrimeDataRecord`                | crime-justice.details      | https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json                 |
| `EducationDetailsRecord`         | education.details          | https://schemas.example.gov.uk/sui/EducationDetailsRecordV1.json          |
| `HealthDataRecord`               | health.details             | https://schemas.example.gov.uk/sui/HealthDataRecordV1.json                |
| `PersonalDetailsRecord`          | personal.details           | https://schemas.example.gov.uk/sui/PersonalDetailsRecordV1.json           |


## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) version 10.0.102 or later
- [.NET runtime](https://dotnet.microsoft.com/download/dotnet/9.0) 9.0.x (required for `dotnet pwsh` and other local tools until PowerShell 7.6 is released with .NET 10 support)
- Container runtime for local dependencies (suggested: [Rancher Desktop](https://rancherdesktop.io) with the Docker Engine option enabled. Recommended if you need a GUI for Docker)

### Setup

To set up the development environment, restore the required .NET tools:

```bash
dotnet tool restore
dotnet husky install
```

This will install the tools specified in `.config/dotnet-tools.json`, including CSharpier for code formatting and Husky.Net for pre-commit hooks to ensure consistent code style.

Also, ensure the .NET self-signed certificate is installed (to enable HTTPS use locally):
```bash
dotnet dev-certs https --trust
```
If encountering problems with the .NET dev certificate, run `dotnet dev-certs https --clean` first, then run `dotnet dev-certs https --trust`.


## Quick Run

1. Complete the Getting Started steps above.
2. Start local dependencies (Azurite) from the repo root:
    ```bash
    docker compose up -d
    ```
    To include an observability stack, use a profile (this also starts Azurite):
    ```bash
    docker compose --profile aspire up -d
    docker compose --profile grafana up -d
    ```
3. Follow the app-specific README to run locally (for example, `Apps/Find/README.md`).

### Local OpenTelemetry

To view local traces and logs from any app, start one of the observability profiles:

```bash
docker compose --profile grafana up -d
```

Or:

```bash
docker compose --profile aspire up -d
```

Note: both profiles bind host port 4317 for OTLP gRPC, so run one at a time unless you change the port mapping in `compose.yaml`.

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


## CI workflows

Workflow structure and inputs are documented in [Docs/Developers/ci-workflows.md](./Docs/Developers/ci-workflows.md). Self-hosted runner and Azure artifact storage details (including the rate-limit workaround and switchback flags) are in [Docs/Developers/ci-self-hosted-runner.md](./Docs/Developers/ci-self-hosted-runner.md).


## Repository structure

Each solution is self-contained and follows a consistent structure:

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
2. Run the test and coverage scripts (You can run the wrapper script for each solution i.e.: `dotnet pwsh <relative path to the solution scoped script>`)
    ```
    dotnet pwsh ./Apps/Find/test_and_cover_find.ps1
    dotnet pwsh ./Apps/StubCustodians/test_and_cover_stubcustodians.ps1
    ```


## Set up Private Nuget Feed

We are using GitHub Packages for hosting API Clients, this feed requires you to sign in to be able to do a restore.
You will need to get a 'Personal Access Token (Classic)' from GitHub.

click on your photo in the top right > click Settings > Developer Settings > Personal access tokens > Tokens (classic) >
click generate new token (classic) > add a note to describe the token > set an expiration date >
check the 'write:packages' permission > click Generate token.

Save a copy of the token on your machine in case you need it in the future.

### JetBrains Rider IDE

When you try and restore a project for the first time that is using a GitHub package feed, Rider will bring up a login box.
Put your GitHub username in the username field and your Token in the password field.

### CLI

If using the `dotnet` cli tool, you can set the `GITHUB_USERNAME` and `GITHUB_TOKEN_DFENUGET` environment variables (referenced in the `nuget.config` files) to specify the credentials for the nuget feed.

Example:

```
GITHUB_USERNAME=YourGitHubUsername GITHUB_TOKEN_DFENUGET=YourTokenHere dotnet watch run --launch-profile https
```
