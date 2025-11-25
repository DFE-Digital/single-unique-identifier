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
```

