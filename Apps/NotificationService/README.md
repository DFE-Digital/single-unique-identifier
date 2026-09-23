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
| `MailboxBaseUrl` | Base URL of the MESH instance. |
| `MailboxId` | The mailbox to read from. |
| `MailboxPassword` | Mailbox password used to build the `NHSMESH` authorisation header. |
| `SharedKey` | Shared key used to HMAC that header. |

All four are required and validated at startup, so a missing or malformed value fails the run
immediately rather than at the first request. `appsettings.Development.json` points at the
[local MESH sandbox](#local-mesh-sandbox), whose self-signed certificate is trusted only in the
`Development` environment.

Do not commit secrets to the configuration files. Supply sensitive local values through environment variables or an approved secret-management mechanism when later workstreams introduce them.

### Message payloads

The mailbox receives [`pds-record-change-2` events](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service/pds-change-event)
published by the NHS Multicast Notification Service (MNS). The MESH workflow identifier they arrive
under is **TBA** - it is not yet known. Each body is a FHIR Bundle (`type: history`) wrapping a
`Parameters` resource that conforms to the R4 Subscriptions Backport `SubscriptionStatus` profile.

The MESH boundary returns each body as raw text; the application layer parses it into a typed
`Hl7.Fhir.Model.Bundle` with the Firely FHIR SDK (`Hl7.Fhir.R4`) and checks it is the expected
shape: a `history` Bundle whose first entry is a `Parameters` resource declaring the
[R4 Subscriptions Backport `SubscriptionStatus` profile](http://hl7.org/fhir/uv/subscriptions-backport/StructureDefinition/backport-subscription-status-r4).
Parsing is deliberately an application concern rather than a transport one, because what to do
with an unusable payload is an acknowledgement decision.

The payload carries an NHS number in its `additional-context.subject` part, so the body is never
logged. A processed message is identified in the logs by its MESH message identifier, event type,
event number and version identifier only. Turning the notification into a supplier broadcast
belongs with the supplier webhook workstream.

The service does not filter on the workflow identifier, so the mailbox is assumed to carry these
events only.

#### Messages that cannot be parsed

A message whose body is not a valid pds-record-change-2 notification is logged and skipped,
and the rest of the mailbox is still read. It will **not** be acknowledged even once supplier
webhook delivery exists, so MESH redelivers it rather than the change event being silently
dropped. The cost is that an unparseable message is re-read, re-logged and re-skipped on every
scheduled run until someone intervenes, so a repeated `could not be parsed and was left
unacknowledged` entry needs a human.

To put a realistic event into the [local MESH sandbox](#local-mesh-sandbox) without running this
application, use `scripts/send-mesh-test-message.ps1`, which posts the payload in
`scripts/pds-record-change-2-notification.template.json`:

```bash
dotnet pwsh ./scripts/send-mesh-test-message.ps1
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