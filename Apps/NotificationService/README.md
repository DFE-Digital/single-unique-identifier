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

## Container image

Build the same image used by CI from the repository root:

```bash
docker build --file Apps/NotificationService/Dockerfile --tag notification-service:local .
```

The runtime container uses the non-root .NET application user and returns the application's exit code.

## Azure deployment

The Notification Service runs as an Azure Container Apps scheduled job. The initial schedule is `0 6 * * *`, which runs once each day at 06:00 UTC. The schedule, CPU, memory, timeout, retry limit and .NET environment are configured independently in each file under `terraform/environments`.

The deployment uses:

- The shared Azure Container Registry and internal-only Container Apps environment from `terraform/core`.
- A service-owned storage account and `SupplierWebhooks` table.
- A user-assigned managed identity for ACR image pulls and Table Storage access.
- A private Table Storage endpoint and private DNS zone linked to the Container Apps VNet.
- The existing Log Analytics workspace for container console logs.

The Storage Account's public firewall is deny-by-default, with the Azure trusted-services bypass enabled. The job reaches Table Storage through its private endpoint, while the `SupplierWebhooks` table is provisioned through Azure Resource Manager rather than the public Storage data plane. No registry credentials or Table Storage keys are supplied to the application.

### First deployment to an environment

The shared foundations must be applied before the first image can be published. Run the `Terraform Core Infrastructure` workflow for the target environment with `apply` enabled. When preparing d01 before this branch is merged, run that existing workflow against this branch ref so the new core resources are available before the automatic main deployment.

The core deployment creates the dedicated Container Apps subnet and changes the shared environment to a VNet-integrated workload profiles environment. If the target already has a Container Apps environment, review the Terraform plan with the platform owner and coordinate any required replacement before applying it. Do not apply this core change while unrelated Container Apps workloads still depend on the existing environment.

After core succeeds:

1. Merging to `main` builds, publishes and deploys the current commit to d01 automatically.
2. For d02 or d03, run the `Build, Test, Deploy: Notification Service` workflow manually and select the environment.
3. The deployment applies the service Terraform, starts an on-demand smoke-test execution, waits for success and verifies its lifecycle logs in Log Analytics.

The GitHub OIDC identity needs permission to create the `AcrPush`, `AcrPull` and `Storage Table Data Contributor` role assignments. If it does not have that permission, an Azure platform administrator must create or delegate those assignments before the first deployment.

### Manual execution and logs

Use the Azure Portal to start an on-demand job execution, review its execution history and inspect its console logs. The deployment workflow also starts and verifies a smoke-test execution automatically.

Do not start a manual execution while another execution is running. Scheduled executions cannot overlap because the 30-minute replica timeout is shorter than the daily interval. Console logs are available in the shared Log Analytics workspace in the `ContainerAppConsoleLogs_CL` table.

### Rollback

Container images are published with immutable, full commit SHA tags. To roll back:

1. Find the last known-good image tag in ACR or a previous successful workflow run.
2. Manually run `Build, Test, Deploy: Notification Service` for the target environment and enter that 40-character lowercase SHA in `image_tag`.
3. The workflow verifies the image exists, applies the previous tag to the job and runs the same smoke test.

Rolling back the image does not change the shared core infrastructure or delete job execution history.

## Supplier Webhook Register Administration

The Supplier Webhook Register is administered manually via Azure Table Storage. There is no public API or UI for this register. Because the Storage Account has no public data-plane access, administrators must use a workstation or jump host connected to the Container Apps VNet (or an approved connected network) when using Azure Storage Explorer or the Azure Portal.

**Table Name:** `SupplierWebhooks`

### Adding a new Supplier Webhook
1. Open Azure Storage Explorer or the Azure Portal.
2. Navigate to the `SupplierWebhooks` table.
3. Add a new Entity with the following strict properties:
    * `PartitionKey`: `SupplierWebhook` (String, Exact match required)
    * `RowKey`: The unique Supplier ID (String)
    * `EndpointUrl`: The supplier's webhook URL (String, **Must be HTTPS**)
    * `IsEnabled`: `true` (Boolean)
    * `ContractVersion`: `1` (String)
    * `SecretKeyVaultReference`: The Key Vault URI/name for their HMAC secret (String)

### Updating or Disabling a Webhook
* To **disable** broadcasts to a supplier, edit their entity and change `IsEnabled` to `false`. (Do not delete the row, to preserve the audit trail).
* To **update** a URL or Key Vault reference, edit the respective string values and save. Changes take effect immediately.
