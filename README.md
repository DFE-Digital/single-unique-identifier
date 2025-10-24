# Single Unique Identifier

This repository is a mono repo that contains multiple .NET solutions, each organised under its own directory.
.NET solutions follow the [Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture) principle ensuring seperation of concerns, maintainability and testability.

To view our documentation, please visit [Single unique identifier](https://dfe-digital.github.io/single-unique-identifier/) github pages

| Directory/File                     | Description                                                                                                 |
|------------------------------------|-------------------------------------------------------------------------------------------------------------|
| [Docs](/Docs)                      | Programme technical documentation, including architecture models and decisions.                             |
| [SystemTests](/SystemTests/)       | .NET solution, providing Gherkin feature definitions of functional requirements covering the entire system. |
| [LICENCE](/LICENCE)                | Standard DfE software licence <!-- Yes, that is spelled correctly. -->, applying to the entire system.      |
| [Contributing](/CONTRIBUTING.md)   | Contributions guide for this repisitory. Please read before contributing                                    |


## Repository structure
Each solution is self-contained and follows a consistant structure:

```
projectName/
  src/
    yourCsProjects/
  tests/
    yourCsTestProject.Unit.Tests/
    yourCsTestProject.Integration.Tests/
```

## Clean architecture
Each solution is structured according to Clean Architecture principles

- Domain - Core business logic and entities.
- Application - Application logic
- Infrastructure - External concerns (e.g. database, third party API calls)
- Presentation/API - UI, API/Endpoints
