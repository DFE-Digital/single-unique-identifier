# Get an Identifier

The 'Get an Identifier' app currently contains Azure Functions.

The Get an Identifier API is part of the Single Unique Identifier (SUI) programme
for children's social care.

Get an Identifier is the main vendor-facing API for the programme's Alpha direction. It searches NHS England's PDS FHIR endpoint using supplied demographics and returns the matched NHS number and GP practice ODS code where available.

The current request path does not create MNS subscriptions. The Alpha direction is to subscribe matched people to MNS, with lifecycle changes received through MESH and processed by the separate [Notification Service](../NotificationService/README.md). Suppliers receive change notifications and rematch through Get an Identifier rather than receiving replacement identity or demographic information in webhooks.

For the current runtime flow and the boundary between implemented and proposed functionality, see the [Get an Identifier as-built design](../../Docs/Design/GetAnIdentifier/AsBuilt.md).

## Running Locally - Recommended Approach

The recommended approach to running the 'Get an Identifier' app locally is:

### One-off prerequisite steps

1. Ensure the prerequisites defined in the [Repo root README](../../README.md) have been installed,
   primarily the .NET SDK and `dotnet tool restore` from the repo root.
2. Install [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools/blob/v4.x/README.md#installing)
3. Create the `local.settings.json` file:
   - `cp ./Apps/GetAnIdentifier/src/SUI.GetAnIdentifier.API/example.local.settings.json ./Apps/GetAnIdentifier/src/SUI.GetAnIdentifier.API/local.settings.json`
4. To run Azurite locally in a container, install Rancher Desktop selecting the **Docker Engine** option during install:
   - Download from <https://rancherdesktop.io>
   - Rancher Desktop is a free, open-source application for running Docker images and does not require a license to use for commercial purposes.
   - Docker is only required to run Azurite as a local container. Azurite provides the dependencies to run Azure Functions locally. There are other approaches to [running Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite) that do not require Docker.
5. If using Rider, install [Azure Toolkit for Rider](https://plugins.jetbrains.com/plugin/11220-azure-toolkit-for-rider)

### PDS FHIR setup steps

- Follow the [PDS FHIR setup guide](./pds_fhir_local_setup.md) to connect to PDS locally.
  - **Important:** The `.env` file contains secrets. Keep it out of source control and AI ingestion tools.

#### PDS FHIR setup - troubleshooting

You may need to remove these lines from your Get an Identifier API's `local.settings.json`:

```
    "NhsAuthConfig:NHS_DIGITAL_CLIENT_ID": "",
    "NhsAuthConfig:NHS_DIGITAL_KID": "",
    "NhsAuthConfig:NHS_DIGITAL_PRIVATE_KEY": "",
```

Ensure that your `.env` file is located in the same directory as your Get an Identifier API's `local.settings.json` file.

Also, Windows only, ensure the line endings in your `.env` file are LF, not CRLF.

### To run locally

#### Run Azurite

```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name sui-azurite mcr.microsoft.com/azure-storage/azurite
```

#### Run AuthEmulator

Get an Identifier validates bearer JWTs using OIDC discovery. For local development, start the repo's AuthEmulator:

```bash
dotnet run --project Apps/AuthEmulator/src/SUI.AuthEmulator/SUI.AuthEmulator.csproj --launch-profile https
```

The default Get an Identifier settings use AuthEmulator at `https://localhost:7250`. Its synthetic clients are defined in [`Data/auth-clients-inbound.json`](../../Data/auth-clients-inbound.json) and include the required `get-an-identifier.read` scope.

#### Run Get an Identifier

Load the local PDS credentials and start the Function App:

```bash
cd Apps/GetAnIdentifier/src/SUI.GetAnIdentifier.API
source .env
func start
```

The protected operation is `POST /api/v1/get-an-identifier`. Calls require a valid bearer JWT and the `get-an-identifier.read` permission. Permissions are resolved from the configured auth store or token scopes according to `AuthSettings:UseAuthStoreForAuthorisation`.

## Logging Guidelines & Data Sanitization

To comply with data protection standards, `ILogger` outputs (Application Insights) must strictly avoid recording Personally Identifiable Information (PII) or secrets.

**Never log the following:**

- Demographic data (Names, Birthdates, Postcodes, Emails, Phones).
- NHS Numbers (neither from requests nor PDS responses).
- Raw JSON request/response bodies or parsing exception messages (e.g., `JsonException.Message`).
- Upstream API diagnostics (e.g., PDS `OperationOutcome.Diagnostics`), as they frequently reflect back requested PII.
- Authentication credentials, tokens, or API keys.

**Safe fields to log:**

- `CorrelationId` and `X-Request-ID`.
- HTTP Status Codes (e.g., `404`, `502`).
- Match scoring values (`Score`) and Match Thresholds.
- Service states and event names (e.g., "Person matched successfully", "PDS API Timeout").

_Note: Complete audit trails containing demographic request bodies are saved securely to Blob Storage via `IAuditLogService`, but must never be passed to `ILogger`._
