# Notification Service

The Notification Service is a run-to-completion .NET application intended for scheduled execution. Each invocation creates an application scope, runs the notification orchestration once, and then exits.

Each execution reads every message waiting in the NHS MESH mailbox and parses the FHIR Bundle it carries. Every message is a [`pds-record-change-2` event](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event): a FHIR Bundle describing a change to a PDS record, such as a change of NHS number. Subscribing to those events is owned by another service; this one only receives what lands in the mailbox.

A message is to be acknowledged - and so removed from the mailbox - only once the change has been successfully delivered to suppliers by webhook. Supplier webhook delivery is not yet implemented, so at present no message is acknowledged and every message stays in the mailbox to be read again on the next run.

## Project boundaries

- `SUI.NotificationService` is the executable host and composition root. It configures the application and invokes one execution.
- `SUI.NotificationService.Application` owns orchestration and the contracts used to coordinate the other modules.
- `SUI.NotificationService.Mesh` is the boundary for receiving messages from an NHS MESH mailbox. It implements `IMeshInboxClient` over the MESH REST API and owns transport only - orchestration decides when messages are read and acknowledged.
- `SUI.NotificationService.Webhooks` is the boundary for delivering notifications to suppliers.
- `SUI.NotificationService.Infrastructure` is the boundary for shared technical concerns needed by the other modules.

The Webhooks project currently exposes a dependency-injection registration point without concrete services. Its implementation will be added by its owning workstream.

This process is not a poller. It reads whatever is waiting, processes it and exits; the schedule that starts the process owns how often that happens.

## Prerequisites

- .NET SDK 10.0.102 or later, as configured in the repository's `global.json`.
- Docker or Podman (or another container runtime), to run the local [NHS MESH sandbox](https://github.com/NHSDigital/mesh-sandbox) that stands in for a real MESH mailbox during local development.

## Local MESH sandbox

Local development and CI never connect to a live MESH environment. Instead they use `mesh_sandbox`, a
local container built from NHS Digital's [mesh-sandbox](https://github.com/NHSDigital/mesh-sandbox)
(pinned to `v1.0.114` in the repository's `compose.yaml`) that simulates the MESH API. Start it from
the repository root before running the application:

```bash
docker compose up -d mesh_sandbox
```

The sandbox listens on `https://localhost:8700` with a self-signed certificate and uses the shared
key `TestKey`, which matches `appsettings.Development.json`. It exposes a `/health` endpoint used by
the container health check:

```bash
curl -k https://localhost:8700/health
```

The sandbox stores messages in memory by default, so they are lost when the container restarts. To
keep them across restarts, uncomment `STORE_MODE=file` in `compose.yaml`.

## Run one execution locally

With the [local MESH sandbox](#local-mesh-sandbox) running, from the repository root run:

```bash
dotnet run --project Apps/NotificationService/src/SUI.NotificationService/SUI.NotificationService.csproj
```

The launch profile sets `DOTNET_ENVIRONMENT=Development`, which is what points the application at
the sandbox. The scaffold logs the start and completion of the execution, then exits with code `0`.

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

### NHS MESH

Messages are read from an NHS MESH mailbox, configured under the `NhsMeshConfig` section:

| Setting | Meaning |
|---------|---------|
| `MailboxBaseUrl` | Base URL of the MESH instance. Must be `https://`; startup fails otherwise. |
| `MailboxId` | The mailbox to read from. |
| `MailboxPassword` | Mailbox password used to build the `NHSMESH` authorisation header. |
| `SharedKey` | Shared key used to HMAC that header. |
| `AcceptLocalDevCert` | Optional, default `false`. Accepts the local sandbox's self-signed certificate. Startup fails if this is `true` and `MailboxBaseUrl` is not a loopback address. |

All four are required and validated at startup, so a missing or malformed value fails the run
immediately rather than at the first request. `appsettings.Development.json` points at the
[local MESH sandbox](#local-mesh-sandbox), whose self-signed certificate is trusted only in the
`Development` environment.

> [!IMPORTANT]
> The client currently works only against the local sandbox. Real MESH environments (INT and LIVE)
> require mutual TLS with an NHS-issued client certificate, and the client does not present one yet,
> so connections to them will fail during the TLS handshake. Client certificate support (loading
> the certificate and key from Key Vault, plus any CA chain MESH needs) is deferred until there is a
> deployed environment. See [ADR 0001](../../Docs/architecture/decisions/System/GetAnIdentifier/0001-NHS-MNS-integration.md).

#### Deployed configuration

The deployed Container Apps job receives these values from GitHub environment configuration
(per environment `d01`–`d03`), passed through `terraform-plan-and-apply.yml` as Terraform variables:

| GitHub | Terraform variable | App setting |
|--------|--------------------|-------------|
| variable `NHS_MESH_CONFIG_BASE_URL` | `nhs_mesh_mailbox_base_url` | `NhsMeshConfig__MailboxBaseUrl` |
| secret `NHS_MESH_CONFIG_SHARED_KEY` | `nhs_mesh_shared_key` | `NhsMeshConfig__SharedKey` |
| secret `NHS_MESH_CONFIG_MAILBOX_ID` | `nhs_mesh_mailbox_id` | `NhsMeshConfig__MailboxId` |
| secret `NHS_MESH_CONFIG_MAILBOX_PASSWORD` | `nhs_mesh_mailbox_password` | `NhsMeshConfig__MailboxPassword` |

The three secrets are stored as Container App job secrets and referenced by the container's
environment. Terraform also sets `NhsMeshConfig__AcceptLocalDevCert=false`, overriding
`appsettings.Development.json`, which the deployed `Development` environment would otherwise inherit.

The PR container validation does not use these values: it starts the local MESH sandbox on the runner
and runs the image with the `Development` configuration.

Do not commit secrets to the configuration files. Supply sensitive local values through environment variables or an approved secret-management mechanism when later workstreams introduce them.

### Message payloads

The mailbox receives [`pds-record-change-2` events](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event)
published by the NHS Multicast Notification Service (MNS). The MESH workflow identifier they arrive
under is **TBA** - it is not yet known. Each body is a FHIR Bundle (`type: history`) wrapping a
`Parameters` resource that conforms to the R4 Subscriptions Backport `SubscriptionStatus` profile.

The MESH boundary returns each body as raw text; the application layer parses it into a typed
`Hl7.Fhir.Model.Bundle` with the Firely FHIR SDK (`Hl7.Fhir.R4`) and checks it is the expected
shape: a `history` Bundle whose first entry is a `Parameters` resource with
`additional-context.event-type` of exactly `pds-record-change-2`. Other MNS events share the same
Bundle and `Parameters` shape, so the event type is checked defensively to stop a subscription
mistake being treated as a PDS record change. The declared `meta.profile`
is deliberately not checked: every Subscriptions Backport notification declares the same profile, so
it cannot tell a pds-record-change-2 event from any other, and an exact match on it only adds a way
to reject valid messages. Parsing is deliberately an application concern rather than a transport one, because what to do
with an unusable payload is an acknowledgement decision.

The payload carries an NHS number in its `additional-context.subject` part, so the body is never
logged. A processed message is identified in the logs by its MESH message identifier, event type,
event number and version identifier only. Turning the notification into a supplier broadcast
belongs with the supplier webhook workstream.

The service does not filter on the workflow identifier, so the mailbox is assumed to carry these
events only; the `event-type` check above is the backstop if that assumption is ever wrong.

#### Messages that cannot be parsed

A message whose body is not a valid pds-record-change-2 notification, or whose
`additional-context.subject` is missing or is not a valid NHS number (ten digits, not starting with
zero, with a valid Modulus 11 check digit, checked by `SUI.Shared.NhsNumberValidator`), is logged and skipped,
and the rest of the mailbox is still read. It will **not** be acknowledged even once supplier
webhook delivery exists, so MESH redelivers it rather than the change event being silently
dropped. The cost is that an unparseable message is re-read, re-logged and re-skipped on every
scheduled run until someone intervenes, so a repeated `could not be parsed and was left
unacknowledged` or `carried no valid NHS number and was left unacknowledged` entry needs a human.

To put a realistic event into the [local MESH sandbox](#local-mesh-sandbox) without running this
application, use `scripts/send-mesh-test-message.ps1`, which posts the payload in
`scripts/pds-record-change-2-notification.template.json`:

```bash
pwsh ./scripts/send-mesh-test-message.ps1
```

The script's default workflow identifier, `PDSRECORDCHANGE_2`, is a placeholder while the real one
is TBA; override it with `-WorkflowId` if needed.

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

1. Replace the temporary `if: ${{ false }}` gate on the Notification Service image-publish job with `if: github.event_name != 'pull_request'` only after the required Azure RBAC delegation is available. Until then, pull requests and `main` builds still validate the service, but image publishing, Terraform apply and smoke testing are skipped.
2. Merging to `main` then builds, publishes and deploys the current commit to d01 automatically.
3. For d02 or d03, run the `Build, Test, Deploy: Notification Service` workflow manually and select the environment.
4. The deployment applies the service Terraform, starts an on-demand smoke-test execution, waits for success and verifies its lifecycle logs in Log Analytics.

The GitHub OIDC identity needs permission to create the `AcrPush`, `AcrPull` and `Storage Table Data Contributor` role assignments. While that delegation is pending, `notification_service_acr_push_enabled` remains `false` in each environment tfvars file, so core infrastructure can still be applied without creating the `AcrPush` assignment. Set it to `true` only once the deployment identity can create role assignments. The Notification Service workflow has a separate temporary deployment gate.

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
