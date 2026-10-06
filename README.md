# Single Unique Identifier

This repository is a mono repo that contains multiple .NET solutions, each organised under its own directory.
The .NET solutions follow the [Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture) principle ensuring separation of concerns, maintainability and testability.

To view our technical documentation, please visit the [Docs](./Docs/index.md) directory.

Looking to getting started with local development? Skip to [Getting Started](#getting-started).

| Directory/File                             | Description                                                                                           |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------------- |
| [Apps](./Apps)                             | The Apps and Components created for the single unique identifier programme.                           |
| [Docs](./Docs)                             | Programme technical documentation, including architecture models and decisions.                       |
| [Public documentation site](./public-docs) | DfE-branded site for approved public integration and onboarding guidance.                             |
| [LICENCE](./LICENCE)                       | Standard DfE software licence<!-- Yes, that is spelled correctly. -->, applying to the entire system. |
| [Contributing](./CONTRIBUTING.md)          | Contributions guide for this repository. Please read before contributing.                             |

## What is 'Single Unique Identifier'?

Today, information about a child is distributed across many independent systems — in education, health, children’s social care, police, youth justice, early years and more. When these systems cannot communicate or reliably match records, important details can be missed, duplicated, or delayed.

This project investigates the technical foundations required to help practitioners improve safeguarding and welfare of children by accessing the right information at the right time, while maintaining strong standards of privacy, security, and data minimisation.

The programme is now working towards **Alpha**. Code, infrastructure, tests, security controls, operability, observability and documentation are treated as production-grade engineering work. Alpha includes learning and iteration; temporary or experimental approaches must be explicitly identified rather than assumed to apply to the whole repository.

### Current service direction

**Get an Identifier** is the main vendor-facing API. Vendors (system suppliers) send demographic data to the service, which searches NHS England's Personal Demographics Service (PDS) and returns the matched NHS number and GP practice ODS code where available.

The agreed Alpha lifecycle flow is:

1. Get an Identifier subscribes matched people to NHS England's Multicast Notification Service (MNS).
2. MNS delivers relevant lifecycle changes through MESH (Message Exchange for Social Care and Health).
3. The separate **Notification Service** processes those notifications and notifies registered supplier webhook endpoints that previously returned information may have changed.
4. Suppliers rematch affected records through Get an Identifier. Webhook notifications do not provide replacement NHS numbers, GP details or demographic information.

This direction is not yet implemented end to end:

| Area                      | Current state on `main`                                                                                                                                                                                                       |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Get an Identifier         | PDS matching and the NHS number / GP practice ODS code response are implemented. MNS subscription creation is not yet implemented.                                                                                            |
| MNS and MESH              | MESH is the accepted notification transport. Duplicate-subscription cleanup is an accepted approach, not an implemented task.                                                                                                 |
| Notification Service      | The orchestrator retrieves and parses MESH messages. Supplier webhook register and signed delivery components exist but are not invoked; webhook dispatch and MESH acknowledgement are not yet wired into the lifecycle flow. |
| Supplier webhook contract | Draft, pending acknowledgement-timeout agreement and technical / information-governance reviews.                                                                                                                              |

See the [Get an Identifier as-built design](./Docs/Design/GetAnIdentifier/AsBuilt.md), [Notification Service README](./Apps/NotificationService/README.md), [accepted MNS decisions](./Docs/architecture/decisions/index.md#accepted-get-an-identifier-decisions) and [draft supplier lifecycle contract](./Docs/Design/Notifications-Webhooks/SupplierLifecycle/V1/Index.md) for the evidence and detailed boundaries.

Earlier `MATCH`, `FIND`, `FETCH`, distributed discovery, custodian polling and jobs designs remain as historical or proposed architectural material. They do not define the current primary Alpha service scope. MESH mailbox polling and scheduled Notification Service execution are separate from that earlier distributed discovery model.

### Security, Trust, and Privacy

A core principle of this work is that **safeguarding information must be protected**.  
Alpha engineering therefore requires:

- Data minimisation
- Clear audit logging
- Strong authentication and role‑based access
- Privacy‑by‑design throughout the architecture
- No central store of case data
- Only the minimum metadata required to support safe decision‑making

These safeguards apply to the implemented service and to work extending it for Alpha. Follow app-specific logging and audit rules; never put demographic data, NHS numbers, secrets or raw sensitive payloads in application logs.

## Glossary of Terms

### Organisation (a.k.a. Agency)

- Organisations are agencies or public bodies involved in safeguarding and protecting children. Specifically, these include the
  police, local authorities, and health services. They also include organisations and agencies that provide placements for
  children, for example: foster and residential care, probation services, youth offending services, early education and childcare
  settings, schools, colleges and other education providers.
- In this codebase, Organisations are also referred to as Providers.

The Searcher and Custodian terms below describe the earlier distributed discovery model; they do not imply current Alpha API capabilities.

### Searcher

- A Searcher is an Organisation that is performing a search for data to make decisions related to safeguarding and protecting
  children. A Searcher is always an Organisation, but is not necessarily a Custodian of a specific child's information.

### Custodian

- A Custodian is an Organisation that holds information, which may include data related to a specific child.

### Supplier

- In the context of multi-agency information sharing, a Supplier is a business that provides systems to Organisations.
- In this codebase, a Supplier is not considered an Organisation, a Custodian, or a Searcher. However, Suppliers do take part in
  facilitating information sharing by providing systems, data storage and connectivity.

## Glossary of Components

### `Get an Identifier`

- Main vendor-facing API: search PDS using demographics and return the matched NHS number and GP practice ODS code where available. MNS subscription creation is part of the Alpha direction, not the current request path.

### `Notification Service`

- Separate service intended to process MNS lifecycle notifications received through MESH and notify registered supplier webhooks so suppliers can rematch. Delivery components exist, but the end-to-end flow is not yet implemented.

### `Auth Emulator`

- Provides a local version of an generic authentication provider which can be used for testing and local development.

### `UI Harness`

- Lightweight UI built to test interaction with the Get an Identifier function.

## Record Types Reference

These record types relate to earlier record-discovery / exchange designs, not the current Get an Identifier response contract.

| C# Type Name                     | Record Type ID             | Schema URI                                                               |
| -------------------------------- | -------------------------- | ------------------------------------------------------------------------ |
| `ChildrensServicesDetailsRecord` | childrens-services.details | https://schemas.example.gov.uk/sui/ChildrensServicesDetailsRecordV1.json |
| `CrimeDataRecord`                | crime-justice.details      | https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json                |
| `EducationDetailsRecord`         | education.details          | https://schemas.example.gov.uk/sui/EducationDetailsRecordV1.json         |
| `HealthDataRecord`               | health.details             | https://schemas.example.gov.uk/sui/HealthDataRecordV1.json               |
| `PersonalDetailsRecord`          | personal.details           | https://schemas.example.gov.uk/sui/PersonalDetailsRecordV1.json          |

## Productionisation

Production-grade engineering is the baseline for Alpha; it does not mean the service is approved or ready to handle real data. Readiness requires assurance of security, privacy, failure handling, testing, observability, operations and rollback across the implemented service.

The existing readiness notes below include dependencies from earlier designs. In particular, mock custodian-directory work is not evidence of current Alpha scope; validate its relevance before treating it as a delivery requirement.

Recorded readiness items include:

- Review all occurrences of `#trivy:ignore`. They must be removed and resolved correctly before handling any real data.
- Update the mock Custodian Service (Org Directory) and mock Auth Store to be real.
  - The goal here is for DfE IT Operations to be able to securely configure the SUI System without them needing to make code changes nor redeploy the infrastructure or software.
  - The current thinking is that good candidates for the solution are Azure Key Vault or Azure App Configuration.

Other none essential but good-to-do long-term items include:

- Upgrade SonarQube licence, rather than using free tier licence.
  - So that the connection from GitHub to SonarQube can use a machine-machine token or ideally federated credentials.
  - The current SonarQube licence does not allow more advanced connections than a short-lived personal access token.

## Development Environments

The current deployment plan promotes builds sequentially through `d01` → `d02` → `d03`. Later environments receive a promoted build rather than deploying independently from `main`.

| Environment ID | Purpose                                                                                                                                                                     | Deployment trigger                                          |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| `d01`          | **Development environment**. Receives the latest changes from `main` for engineering verification. May contain unstable or incomplete functionality.                        | Automatic deployment from `main`.                           |
| `d02`          | **Controlled testing and demonstration environment**. Provides a more stable build for testing, demonstrations and research, with deployment timing controlled by the team. | Manual promotion of a build successfully deployed to `d01`. |
| `d03`          | **Pre-production environment**. Supports validation before any future production release.                                                                                   | Manual promotion of a build from `d02` only.                |

This is the current environment strategy and may evolve as the programme progresses. The [Get an Identifier authentication matrix](./Docs/Design/GetAnIdentifier/AsBuilt.md#authentication-by-environment) explains the intended gateway and issuer setup for each environment, its purpose, and which settings require deployment verification.

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

Commits also run a GitLeaks pre-commit scan via Husky. The hook will use a pinned GitLeaks `8.30.0` binary and download it into a user cache directory if it is not already available on your machine. The auto-install path currently supports macOS and Linux on x64 and arm64, plus Windows on x64.

If you need to refresh the repository baseline for tracked fixtures, run:

```bash
dotnet pwsh ./scripts/security/run-gitleaks.ps1 -Mode Baseline
```

If GitLeaks blocks a commit and you are certain the finding is expected, update `.gitleaks.toml` or regenerate `.gitleaks.baseline.json`. For urgent one-off commits only, you can bypass the local scan with:

```bash
SUI_SKIP_GITLEAKS=1 git commit
```

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
3. Follow the app-specific README to run locally (for example, `Apps/GetAnIdentifier/README.md`).

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

Security scanning is layered:

- `Trivy IaC Scan` blocks pull requests and pushes to `main` on `HIGH` and `CRITICAL` infrastructure-as-code findings.
- `TruffleHog Secret Scan` blocks pull requests and pushes to `main` on new verified or unknown secret findings.
- `Trivy Repository Scan` and `TruffleHog Deep Secret Scan` run as broader scheduled/manual hygiene scans.

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

## Configuring Non-public Client IDs and Secrets for Authentication

While having client secrets in a public repo is fine for local development and ephemeral environments, deployed environments should use actual secret values (rather than pretend secret values that have been publicly published) so that unauthorised people cannot authenticate with our deployed environments.

This is achieved via the `AuthClientCredentials` configuration functionality that enables overriding the Client IDs and Secrets in the sample data.

Ultimately this is driven by GitHub Environment Secrets called:

- `AUTH_CLIENT_IDS_JSON_MAP`
- `AUTH_CLIENT_SECRETS_JSON_MAP`

Both secrets must be JSON maps, where the key is the original Client ID and the value is the corresponding private value.

`AUTH_CLIENT_IDS_JSON_MAP` expects a map of `OriginalClientId` to `SensitiveClientId`, for example:

```json
{"CLIENT_ID_LOCAL_AUTHORITY_01":"sensitive-client-id-1","CLIENT_ID_EDUCATION_01":"sensitive-client-id-2"}
```

`AUTH_CLIENT_SECRETS_JSON_MAP` expects a map of `OriginalClientId` to `SensitiveClientSecret`, for example:

```json
{"CLIENT_ID_LOCAL_AUTHORITY_01":"sensitive-client-secret-1","CLIENT_ID_EDUCATION_01":"sensitive-client-secret-2"}
```

**It is important to note that these values must be a single line. They must not be multi-line. Newline characters break the GitHub workflows!**

The PowerShell script `scripts/generate-auth-client-credentials-secrets.ps1` exists to help generate the values.

To generate secret values:

```bash
dotnet pwsh ./scripts/generate-auth-client-credentials-secrets.ps1
```

To generate template values:

```bash
dotnet pwsh ./scripts/generate-auth-client-credentials-secrets.ps1 -TemplateMode
```
