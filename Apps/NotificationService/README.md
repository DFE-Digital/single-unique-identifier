# Notification Service

The Notification Service is a run-to-completion .NET application intended for scheduled execution. Each invocation creates an application scope, runs the notification orchestration once, and then exits.

Each execution drains the NHS MESH mailbox: it reads every message waiting in the inbox, then acknowledges it. Every message is a [`pds-record-change-2` event](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event): a FHIR Bundle describing a change to a PDS record, such as a change of NHS number. Subscribing to those events is owned by another service; this one only receives what lands in the mailbox. Supplier webhook delivery is not yet implemented.

## Project boundaries

- `SUI.NotificationService` is the executable host and composition root. It configures the application and invokes one execution.
- `SUI.NotificationService.Application` owns orchestration and the contracts used to coordinate the other modules.
- `SUI.NotificationService.Mesh` is the boundary for receiving messages from an NHS MESH mailbox. It implements `IMeshMessageReceiver` over the MESH REST API and owns transport only - orchestration decides when messages are read and acknowledged.
- `SUI.NotificationService.Webhooks` is the boundary for delivering notifications to suppliers.
- `SUI.NotificationService.Infrastructure` is the boundary for shared technical concerns needed by the other modules.

The Webhooks project currently exposes a dependency-injection registration point without concrete services. Its implementation will be added by its owning workstream.

This process is not a poller. It reads whatever is waiting, acknowledges it and exits; the schedule that starts the process owns how often that happens.

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

### NHS MESH

Messages are read from an NHS MESH mailbox, configured under the `NhsMeshConfig` section:

| Setting | Meaning |
|---------|---------|
| `MailboxBaseUrl` | Base URL of the MESH instance. |
| `MailboxId` | The mailbox to read from. |
| `MailboxPassword` | Mailbox password used to build the `NHSMESH` authorisation header. |
| `SharedKey` | Shared key used to HMAC that header. |

All four are required and validated at startup, so a missing or malformed value fails the run
immediately rather than at the first request. `appsettings.Development.json` points at the local
MESH sandbox in `compose.yaml`, whose self-signed certificate is trusted only in the `Development`
environment.

Do not commit secrets to the configuration files. Supply sensitive local values through environment variables or an approved secret-management mechanism when later workstreams introduce them.

### Message payloads

The mailbox receives [`pds-record-change-2` events](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event)
published by the NHS Multicast Notification Service (MNS). They arrive under the MESH workflow
identifier `PDSRECORDCHANGE_2`, and each body is a FHIR Bundle (`type: history`) wrapping a
`Parameters` resource that conforms to the R4 Subscriptions Backport `SubscriptionStatus` profile.

This service does not yet interpret that payload: it reads the body as a string, logs it and
acknowledges the message. It also does not filter on the workflow identifier, so the mailbox is
assumed to carry these events only. Parsing the Bundle into a domain event belongs with the
supplier webhook workstream.

To put a realistic event into the local MESH sandbox without running this application, use
`scripts/send-mesh-test-message.ps1`, which posts the payload in
`scripts/pds-record-change-2-notification.template.json`:

```bash
dotnet pwsh ./scripts/send-mesh-test-message.ps1
```

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

## Supplier Webhook Register Administration

The Supplier Webhook Register is administered manually via Azure Table Storage. There is no public API or UI for this register.

**Table Name:** `SupplierWebhookEntity`

### Adding a new Supplier Webhook
1. Open Azure Storage Explorer or the Azure Portal.
2. Navigate to the `SupplierWebhookEntity` table.
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