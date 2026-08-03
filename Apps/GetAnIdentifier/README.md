# Get an Identifier

The 'Get an Identifier' app currently contains Azure Functions.

The Get an Identifier API is part of the Single Unique Identifier (SUI) programme
for children's social care.

Get an Identifier provides an adapter for matching a set of demographics with an NHS Number via the PDS Fhir endpoint.

## Running Locally - Recommended Approach

The recommended approach to running the 'Get an Identifier' app locally is:

### One-off prerequisite steps

1. Ensure the prerequisites defined in the [Repo root README](../../README.md) have been installed,
   primarily the .NET SDK and `dotnet tool restore` from the repo root.
2. Install [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools/blob/v4.x/README.md#installing)
3. Create the `local.settings.json` file:
    * `cp ./Apps/GetAnIdentifier/src/SUI.GetAnIdentifier.API/example.local.settings.json ./Apps/GetAnIdentifier/src/SUI.GetAnIdentifier.API/local.settings.json`
4. To run Azurite locally in a container, install Rancher Desktop selecting the **Docker Engine** option during install:
    * Download from <https://rancherdesktop.io>
    * Rancher Desktop is a free, open-source application for running Docker images and does not require a license to use for commercial purposes.
    * Docker is only required to run Azurite as a local container. Azurite provides the dependencies to run Azure Functions locally. There are other approaches to [running Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite) that do not require Docker.
5. If using Rider, install [Azure Toolkit for Rider](https://plugins.jetbrains.com/plugin/11220-azure-toolkit-for-rider)

### PDS Fhir setup steps

- Follow the (Fhir readme)[./pds_fhir_local_setup.md] for connecting locally to PDS.
    - !Important - The .env file contains secrets so keep it away from source control and any AI ingestion tools.

#### PDS Fhir setup - Troubleshooting

You may need to remove these lines from your Get an Identifier API's `local.settings.json`:

```
    "NhsAuthConfig:NHS_DIGITAL_CLIENT_ID": "",
    "NhsAuthConfig:NHS_DIGITAL_KID": "",
    "NhsAuthConfig:NHS_DIGITAL_PRIVATE_KEY": "",
```

Ensure that your `.env` file is located in the same directory as your Get an Identifier API's `local.settings.json` file.

Also, Windows only, ensure the line endings in your `.env` file are LF, not CRLF.

### To run locally

#### Configure x-api-key

The Get an Identifier function requires an `x-api-key` header for authentication. Configure it in your `local.settings.json`:

```json
{
  "Values": {
    "GetAnIdentifierFunction__XApiKey": "local-dev-key-change-me"
  }
}
```

This is the `x-api-key` for invoking our endpoint. It is **not** the key for PDS Fhir.
For local dev, the key is not important, and it is recommended to keep the value as `local-dev-key-change-me`.

In Dev/Test/Prod environments, the key is automatically retrieved from Azure Key Vault (secret name: `find-match-api-key`).
The operational rotation process for this secret is documented in [Docs/Developers/secret-rotation.md](../../Docs/Developers/secret-rotation.md).

#### Run Azurite

```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name sui-azurite mcr.microsoft.com/azure-storage/azurite
```

#### Run 'Find' and the 'Stub Custodians'

Using Rider:

* Run the `Launch Find and Stub Custodians` profile
* Note that [Azure Toolkit for Rider](https://plugins.jetbrains.com/plugin/11220-azure-toolkit-for-rider) needs to be installed

Or, using the command line (from the repo root):

* `cd ./Apps/Find/src/SUI.Find.FindApi/; func start --port 7182`


## Test Data

| Given   | Family   | Birthdate  | Gender | Phone | Email | Postcode | NHS Number | Notes                                                                                                     |
|---------|----------|------------|--------|-------|-------|----------|------------|-----------------------------------------------------------------------------------------------------------|
| Octavia | Chislett | 2008-09-20 | female | null  | null  | KT19 0ST | 9691292211 | should return 6 records when logged in as LOCAL-AUTHORITY-01 and 4 records when logged in as EDUCATION-01 |
| Briar   | Anderton | 2009-02-15 | male   | null  | null  | KT21 1JA | 9449306613 | should return 1 record when logged in as LOCAL-AUTHORITY-01                                               |
| Red     | Flindall | 2011-05-17 | male   | null  | null  | KT20 6XJ | 9449306494 | should return 2 records when logged in as HEALTH-01                                                       |


### End to end
This is a full end to end test example using the swagger ui to execute the requests.

#### Match
Search for a person and get an SUI back
```
/v1/matchperson

{
  "personSpecification": {
    "given": "Octavia",
    "family": "Chislett",
    "birthDate": "2008-09-20",
    "gender": "female",
    "phone": null,
    "email": null,
    "addressPostalCode": "KT19 0ST"
  }
}

Example response
{
  "PersonId": "9691292211"
}
```