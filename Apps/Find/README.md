# Find

The 'Find' app currently contains Azure Functions, including Find a Record and Fetch a Record.

The Find a Record and Fetch a Record APIs are part of the Single Unique Identifier (SUI) programme
for children's social care.

Find a Record provides an asynchronous interface to discover which systems hold
records associated with a given SUI.

Fetch a Record provides an asynchronous interface to fetch records from systems via pointers obtained
from Find a Record.

## Running Locally - Recommended Approach

The recommended approach to running the 'Find' app locally is:

### One-off prerequisite steps

1. Ensure the prerequisites defined in the [Repo root README](../../README.md) have been installed,
primarily the .NET SDK and `dotnet tool restore` from the repo root.
2. Install [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools/blob/v4.x/README.md#installing)
3. Create the `local.settings.json` file:
    * `cp ./Apps/Find/src/SUI.Find.FindApi/example.local.settings.json ./Apps/Find/src/SUI.Find.FindApi/local.settings.json`
    * `cp ./Apps/Find/src/SUI.Find.AuditProcessor/example.local.settings.json ./Apps/Find/src/SUI.Find.AuditProcessor/local.settings.json`
4. To run Azurite locally in a container, install Rancher Desktop selecting the **Docker Engine** option during install:
    * Download from <https://rancherdesktop.io>
    * Rancher Desktop is a free, open-source application for running Docker images and does not require a license to use for commercial purposes.
    * Docker is only required to run Azurite as a local container. Azurite provides the dependencies to run Azure Functions locally. There are other approaches to [running Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-install-azurite) that do not require Docker.
5. If using Rider, install [Azure Toolkit for Rider](https://plugins.jetbrains.com/plugin/11220-azure-toolkit-for-rider)

### PDS Fhir setup steps

- Follow the (Fhir readme)[./pds_fhir_local_setup.md] for using the matching service.
  - !Important - The .env file contains secrets so keep it away from source control and any AI ingestion tools.


### To run locally

#### Configure x-api-key

The MatchPerson function requires an x-api-key header for authentication. Configure it in your `local.settings.json`:

```json
{
  "Values": {
    "MatchFunction__XApiKey": "your-local-dev-key"
  }
}
```

In Dev/Test/Prod environments, the key is automatically retrieved from Azure Key Vault (secret name: `find-api-key`).

#### Run Azurite

```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

#### Run 'Find' and the 'Stub Custodians'

Using Rider:

* Run the `Launch Find and Stub Custodians` profile
* Note that [Azure Toolkit for Rider](https://plugins.jetbrains.com/plugin/11220-azure-toolkit-for-rider) needs to be installed

Or, using the command line (from the repo root):

* `cd ./Apps/Find/src/SUI.Find.FindApi/; func start --port 7182`
* `cd ./Apps/StubCustodians/src/SUI.StubCustodians.API/; dotnet run`
* `cd ./Apps/Find/src/SUI.Find.AuditProcessor/; func start --port 7151` <- Optional to have the audit processor running


## Test Data

### scenario 1

#### Match
```
/v1/matchperson

{
"given": "Octavia",
"family": "Chislett",
"birthDate": "2008-09-20",
"gender": "female",
"phone": null,
"email": null,
"addressPostalCode": "KT19 0ST"
}

response with encryption (LOCAL-AUTHORITY-01)
{
  "PersonId": "gkITssvF1IAbNgpcMv2lyA"
}

response without encryption
{
  "PersonId": "9691292211"
}
```
#### Find - start search
```
v1/searches

{
  "suid": "gkITssvF1IAbNgpcMv2lyA"
}

Example Response
{
  "JobId": "5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5",
  "Suid": "gkITssvF1IAbNgpcMv2lyA",
  "Status": "Queued",
  "CreatedAt": "2026-02-05T09:19:36.690255+00:00",
  "LastUpdatedAt": "2026-02-05T09:19:36.690255+00:00",
  "_links": {
    "self": {
      "Href": "/v1/searches/5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5",
      "Method": "GET"
    },
    "status": {
      "Href": "/v1/searches/5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5",
      "Method": "GET"
    },
    "cancel": {
      "Href": "/v1/searches/5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5",
      "Method": "DELETE"
    }
  }
}
```
#### Find - get results
```
/v1/searches/5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5/results

example response
{
  "JobId": "5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5",
  "Suid": "9691292211",
  "Status": "Completed",
  "Items": [
    {
      "ProviderSystem": "local-authority-01",
      "ProviderId": "local-authority-01",
      "RecordType": "childrens-services.details",
      "RecordUrl": "/v1/records/c62d406e97a14203953617828f423f9d"
    },
    {
      "ProviderSystem": "education-01",
      "ProviderId": "education-01",
      "RecordType": "education.details",
      "RecordUrl": "/v1/records/abf33594cea44454b9bd6ca1d2024ede"
    },
    {
      "ProviderSystem": "education-01",
      "ProviderId": "education-01",
      "RecordType": "personal.details",
      "RecordUrl": "/v1/records/2d9782ef03204778b1a29fc2edb1132e"
    },
    {
      "ProviderSystem": "health-01",
      "ProviderId": "health-01",
      "RecordType": "health.details",
      "RecordUrl": "/v1/records/ed66ba0dce6c48c2aa353b6c4a7850ff"
    },
    {
      "ProviderSystem": "police-01",
      "ProviderId": "police-01",
      "RecordType": "crime-justice.details",
      "RecordUrl": "/v1/records/bdbffadfe59341adb2fd60fc8f8833df"
    }
  ],
  "_links": {
    "self": {
      "Href": "/search/5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5/results",
      "Method": "GET"
    },
    "job": {
      "Href": "/search/5493C9E42891BDDDBDA27C34E24CFF85A3D70CF0FD1FC964559462C26F36FDF5",
      "Method": "GET"
    }
  }
}
```

```
v1/records/2d9782ef03204778b1a29fc2edb1132e

{
  "RecordId": "ATT-3210-1",
  "PersonId": "vehNMF2ySUU23P206A6BYA",
  "RecordType": "personal.details",
  "Version": 1,
  "SchemaUri": "https://schemas.example.gov.uk/sui/personal.details.record.v1.json",
  "Payload": {
    "firstName": "Octavia",
    "lastName": null,
    "dateOfBirth": null,
    "address": {
      "line1": "12 Burton Street",
      "line2": null,
      "townOrCity": "London",
      "county": null,
      "postcode": "SW1A 0AA"
    },
    "namesOfIndividualsResidingAtMainAddress": [
      "James Smith",
      "Henry Smith",
      "Thomas Smith",
      "Jason Archer",
      "Sarah Flint-Smith"
    ],
    "birthAssignedSex": null,
    "pronouns": null,
    "ethnicity": null,
    "firstLanguage": null,
    "designatedLocalAuthority": null,
    "englishAsAdditionalLanguage": null,
    "braille": null,
    "signLanguage": true,
    "makaton": true,
    "interpreter": null,
    "relatedPeople": null
  }
}
```