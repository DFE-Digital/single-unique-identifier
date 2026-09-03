# Notification Service

The Notification Service is a run-to-completion .NET application intended for scheduled execution. Each invocation creates an application scope, runs the notification orchestration once, and then exits.

The current scaffold does not connect to MNS or deliver supplier webhooks.

## Project boundaries

- `SUI.NotificationService` is the executable host and composition root. It configures the application and invokes one execution.
- `SUI.NotificationService.Application` owns orchestration and the contracts used to coordinate the other modules.
- `SUI.NotificationService.Mns` is the boundary for receiving lifecycle changes from MNS.
- `SUI.NotificationService.Webhooks` is the boundary for delivering notifications to suppliers.
- `SUI.NotificationService.Infrastructure` is the boundary for shared technical concerns needed by the other modules.

The MNS, Webhooks and Infrastructure projects currently expose dependency-injection registration points without concrete services. Their implementations will be added by their owning workstreams.

## Prerequisites

- .NET SDK 10.0.102 or later, as configured in the repository's `global.json`.

## Run one execution locally

From the repository root, run:

```bash
dotnet run --project Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj
```

The scaffold logs the start and completion of the execution, then exits with code `0`.

## Local configuration

The application uses the standard .NET configuration sources:

1. `appsettings.json`.
2. `appsettings.{Environment}.json`.
3. Environment variables.
4. Command-line arguments.

Set `DOTNET_ENVIRONMENT` to select an environment-specific configuration file. For example:

```bash
DOTNET_ENVIRONMENT=Development \
dotnet run --project Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj
```

Use double underscores in environment variable names for nested configuration values. For example, to change the default log level:

```bash
Logging__LogLevel__Default=Warning \
dotnet run --project Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj
```

Configuration can also be supplied as command-line arguments:

```bash
dotnet run --project Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj -- \
  --Logging:LogLevel:Default Debug
```

Do not commit secrets to the configuration files. Supply sensitive local values through environment variables or an approved secret-management mechanism when later workstreams introduce them.

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Execution completed successfully. |
| `1` | An unhandled startup, orchestration, shutdown or disposal failure occurred. |
| `2` | Execution was cancelled gracefully. |

Ctrl+C and termination signals request graceful cancellation through the application cancellation token.

## Run the automated tests

From the repository root, run:

```bash
dotnet test Apps/NotificationService/NotificationService.slnx
```
