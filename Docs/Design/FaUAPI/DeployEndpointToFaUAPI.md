# Find and Use API API endpoint manual setup

**Date:** `2026-09-28`  
**Owner:** SUI Service Team  
**Scope:** A guide on how to deploy a new API to Find and Use API (FaUAPI).

---

## 1. Purpose

This guide outlines the step-by-step procedure for configuring, uploading schemas, setting up release versions, environments, authentication, and publishing an API service.

## 2. Prerequisites

Before starting, ensure you have downloaded the schema from the Azure Function or Service:

1. Navigate to `https://<your-azure-app>.azurewebsites.net/api/swagger.json`.
2. Copy the output into a `.json` file and save it with a descriptive name (e.g., `d01-getAnIDSchema.json`).

## 3. Steps

### Step 1: Add New API & Fill Details

1. In the relevant Workspace, navigate to **Overview**, and in the APIs subsection, select **Add**.
2. Select **Hosted**.
3. Complete the **Enter the details of your API** form using the values appropriate for your environment (see example values below):

- **API Name**: `SUI Get An ID d02 - PoC 1 API`
- **API Codename**: `sui-get-an-id-d02-poc1-api`
- **Description**: `The SUI Get an Id API is part of the Single Unique Identifier (SUI) programme for improving multi-agency information sharing in relation to safeguarding and welfare of children. This specific API represents a proof-of-concept for fetching a child's ID.`
- **Backend Type**: Select `HTTP`
- **Major Version Identifier**: `0`
- **Version Scheme**: Select `URL suffix`

1. Click **Save changes**.

### Step 2: Upload OpenAPI Schema

1. Next to "Schema" select **Change**.
2. Select Schema type **Swagger**.

2. Upload the JSON schema file created during the **Prerequisites** step.
3. Click **Apply Schema**.

### Step 3: Configure Metadata

Navigate to the **Metadata** section and update the following settings:

- **Data Exposure Classification:** `Across government services`
- **Project URL:** `https://github.com/DFE-Digital/single-unique-identifier`

### Step 4: Add Initial Release

1. Go to the **Releases** tab.
2. Click **Add** and configure the release settings:

- **Name:** `pre-release-1`
- **Status:** `Planned`
- **Current Release:** Tick
- **Release Date:** Select today's date
- **Release Notes:** `initial release`

### Step 5: Environment Configuration

Navigate to **Environment** and configure as follows:

- **Enabled & Subscribable:** Tick
- **Backend Mode:** `Explicit service URL`
- **Backend URL:** Specify your Azure Function API endpoint:
  - *Format:* `https://<azure-app-name>.azurewebsites.net/api`
  - *Example:* `https://s270d02func-ukw-getanid01.azurewebsites.net/api`
- **Visibility:** Set to `Inherit visibility from API`

### Step 6: Authentication & Scope Setup

1. Go to **Authentication** and set up according to your target environment requirements.
2. Configure settings:

- **Enabled:** Tick
- **Auto Enforcement:** Tick
- **Custom Scope:** `suigetanid{environment}`
- **Delegated Permissions:** Tick
- **OpenID Scope:** Tick

### Step 7: Inbound Policy Configuration

1. Go to **Policies**.
2. Select and edit the **Live Environment** scope policy.
3. Add or update the `<inbound>` block with the following rate-limiting configuration:

```xml
<policies>
 <inbound>
  <base/>
  <rate-limit calls="30" renewal-period="60"/>
 </inbound>
</policies>
```

### Step 8: Validation & Publishing

1. Navigate to **Publish**.
2. Review the configuration status to ensure all fields are valid.
3. Resolve any missing configuration items or validation warnings if prompted.
4. Click **Publish**.
