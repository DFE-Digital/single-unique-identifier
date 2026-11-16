# Runbook: subscribe to MNS PDS record change events

This runbook creates the NHS [Multicast Notification Service (MNS)](https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service)
subscriptions that deliver `pds-record-change-2` events to our MESH mailbox, where
[NotificationService](../../Apps/NotificationService/README.md) reads them.

Subscriptions are created once per environment, by hand, with `subscribe.cs` in this folder.
It was written for INT and is reused for production. The script has two commands:

- `list` shows every subscription our application already has. It is read-only.
- `create` creates the subscriptions below, skipping any that already exist.

## What gets created

Two subscriptions, both delivered to the same MESH mailbox as FHIR JSON (`application/fhir+json`):

| Name              | Criteria                                                     |
| ----------------- | ------------------------------------------------------------ |
| GP change         | `eventType='pds-record-change-2' AND changed_gp=True`        |
| NHS number change | `eventType='pds-record-change-2' AND changed_nhsnumber=True` |

`changed_nhsnumber` is not in the public MNS documentation, but we have been told it exists.

The criteria are defined at the top of `subscribe.cs`. Each request is a FHIR `Subscription`:

```json
{
  "resourceType": "Subscription",
  "status": "requested",
  "reason": "SUI NotificationService - PDS record change (GP change)",
  "criteria": "eventType='pds-record-change-2' AND changed_gp=True",
  "channel": {
    "type": "message",
    "endpoint": "mesh://<MAILBOX_ID>",
    "payload": "application/fhir+json"
  }
}
```

By default no MESH workflow ID is set. The MNS API specification shows the endpoint as
`mesh://<MAILBOX_ID>?workflow_id=<ID>`. If MNS rejects the endpoint, see [Troubleshooting](#troubleshooting).

### Under-18 filter (added later)

We only want events for under-18s, but MNS does not offer an age filter yet. We start with the two criteria above and add the age filter when it is available.

When the filter is available:

1. Get MNS approval for the new filter (see prerequisite 2).
2. Add it to both criteria in `subscribe.cs`, so every environment uses the same criteria.
3. Replace the existing subscriptions in each environment, INT first. Ask MNS whether a subscription's
   criteria can be updated in place. If not:
   1. Run `create`. The existing subscriptions have different criteria, so they are not skipped, and
      new subscriptions are created alongside them.
   2. Run `list` and check the new ones are `active`.
   3. Delete the old ones (see [Rollback](#rollback)).

   Creating the new ones before deleting the old ones means no events are missed. Until the old ones
   are deleted, events for under-19s arrive twice.

## Prerequisites

Do these once per environment, before running the script.

1. **MESH mailbox.** The target mailbox exists (INT uses a Path to Live mailbox), and you know its mailbox ID.
2. **MNS onboarding approved.** MNS has approved our NHS developer portal application for
   `pds-record-change-2` with the filters above. You request approval through the MNS ServiceNow form
   ([subscribe to MNS](https://digital.nhs.uk/services/multicast-notification-service/subscribe-to-the-multicast-notification-service)),
   giving the event, use case, filters and **application ID**. Until it is approved, subscription requests fail with 401 or 403.
3. **MNS API added to the application.** In the [NHS developer portal](https://onboarding.prod.api.platform.nhs.uk/),
   open the application that GetAnIdentifier uses for PDS FHIR, choose **Add APIs** and add the
   Multicast Notification Service API for the environment.
4. **Key Vault access.** You can read secrets from the GetAnIdentifier Key Vault (its name contains `gaidkv01`) with the Azure CLI (`az login`).
5. **.NET SDK.** The version pinned in `global.json` (.NET 10) is installed.

### Why the GetAnIdentifier credentials work

MNS uses the same NHS [application-restricted signed JWT authentication](https://digital.nhs.uk/developer/guides-and-documentation/security-and-authorisation/application-restricted-restful-apis-signed-jwt-authentication)
and the same token endpoint (`/oauth2/token`) as GetAnIdentifier's PDS FHIR calls. Access is granted
per developer portal application. Once prerequisites 2 and 3 are done for that application, its
existing client ID, key ID and private key also work for MNS. `subscribe.cs` signs the token request
the same way as `FhirAuthTokenService` in GetAnIdentifier.

## Steps

All commands run from the repository root. To list the script's options, run
`dotnet run scripts/mns_subscribe/subscribe.cs -- --help` (or `-- create --help`, `-- list --help`).

### 1. Load the credentials

Load them into environment variables without printing them. `subscribe.cs` reads secrets only from
these variables; never pass them as arguments.

```bash
KV_NAME=$(az keyvault list --query "[?contains(name, 'gaidkv01')].name" -o tsv)   # check it is the right environment's vault
echo "$KV_NAME"

export NHS_DIGITAL_CLIENT_ID="$(az keyvault secret show --vault-name "$KV_NAME" --name nhs-digital-client-id --query value -o tsv)"
export NHS_DIGITAL_KID="$(az keyvault secret show --vault-name "$KV_NAME" --name nhs-digital-kid --query value -o tsv)"
export NHS_DIGITAL_PRIVATE_KEY="$(az keyvault secret show --vault-name "$KV_NAME" --name nhs-digital-private-key --query value -o tsv)"
```

If more than one vault matches, set `KV_NAME` to the one for the environment you are subscribing.
For INT, that is the vault whose GetAnIdentifier deployment calls `int.api.service.nhs.uk`.

### 2. Dry run

Check the URLs and request bodies. Nothing is sent and no credentials are needed, so the dry run
does not check for existing subscriptions.

```bash
dotnet run scripts/mns_subscribe/subscribe.cs -- create --env int --mailbox <MAILBOX_ID> --dry-run
```

To check that the script can reach MNS, use the open-access MNS sandbox. It needs no credentials
and returns canned data:

```bash
dotnet run scripts/mns_subscribe/subscribe.cs -- list --env sandbox
```

### 3. Check existing subscriptions

```bash
dotnet run scripts/mns_subscribe/subscribe.cs -- list --env int
```

The output lists each subscription's ID, status, criteria and endpoint:

```text
Access token obtained.
<subscription_id>  active
  Criteria: eventType='pds-record-change-2' AND changed_gp=True
  Endpoint: mesh://<MAILBOX_ID>  (application/fhir+json)
  Reason:   SUI NotificationService - PDS record change (GP change)
1 subscription(s).
```

`create` skips any subscription that already exists, so this step is for checking what is there. For a
subscription that is not `active` (`requested`, `error` or `off`), the script also prints the full
MNS response for it; raise anything other than `active` with MNS.

### 4. Create the subscriptions

```bash
dotnet run scripts/mns_subscribe/subscribe.cs -- create --env int --mailbox <MAILBOX_ID>
```

Expected output:

```text
Access token obtained.
CREATED (201): eventType='pds-record-change-2' AND changed_gp=True
  Subscription ID: <uuid>
  Correlation ID:  <uuid>
CREATED (201): eventType='pds-record-change-2' AND changed_nhsnumber=True
  Subscription ID: <uuid>
  Correlation ID:  <uuid>
```

MNS documents `201 Created`. The script accepts any 2xx status as success and prints it. If it is
not 201, the script also prints the response body: record it, and check with MNS that exactly one
subscription was created before doing anything else.

**Record both subscription IDs and correlation IDs in the ticket.** You need the IDs to change or
delete a subscription, and MNS support asks for correlation IDs.

Before creating anything, the script fetches the existing subscriptions. It skips any with the same
criteria and the same MESH mailbox, ignoring the workflow ID, because a duplicate would mean duplicate
MESH messages. Criteria are compared ignoring quotes, spaces and case. A skipped subscription is
reported like this:

```text
SKIPPED (already exists): eventType='pds-record-change-2' AND changed_gp=True
  Subscription ID: <uuid>  active
  Endpoint:        mesh://<MAILBOX_ID>
```

The script stops at the first failure and exits with code 1:

- `FAILED` (a non-2xx status): nothing was created for that subscription. Fix the cause and re-run;
  the subscriptions already created are skipped.
- `OUTCOME UNKNOWN` (no response, for example a timeout, a dropped connection or Ctrl+C): the request
  may have reached MNS. The script prints the correlation ID. Run `list` to check, then re-run; if the
  subscription was created, it is skipped.

Anything already printed as `CREATED` exists, and `list` shows what MNS has.

### 5. Clear the credentials

```bash
unset NHS_DIGITAL_CLIENT_ID NHS_DIGITAL_KID NHS_DIGITAL_PRIVATE_KEY
```

### 6. Verify delivery

- Run `list` again: both subscriptions should show `active`.
- When a matching PDS change happens, a message arrives in the MESH mailbox. Its MESH subject is
  `<subscription_id>_<event_timestamp>`.
- The next NotificationService run logs the processed MESH message IDs.
- In INT you may have to wait for, or trigger, a matching PDS test-data change. To prove MNS-to-MESH
  connectivity on its own first, temporarily subscribe to `eventType=mns-test-signal-1`, which MNS
  sends every 15 minutes, then delete that subscription. NotificationService ignores events it does
  not recognise and leaves them in the mailbox, so acknowledge or clear those test messages afterwards.

## Troubleshooting

The script prints the HTTP status, correlation ID and MNS response body for a failed request.

| Symptom                                       | Likely cause                                                                                    |
| --------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `Token request failed: 400` or `401`          | Wrong client ID, key ID or private key, or the key is not registered for this environment.      |
| Subscription `401` or `403`                   | MNS onboarding not approved yet, or the MNS API not added to the application (prerequisites 2–3). |
| Subscription `400` mentioning `channel.endpoint` | MNS requires a workflow ID. Re-run with `--workflow-id <ID>` (letters, digits, `_`, `.`, `-`). No registration is needed; it is a label MNS puts on delivered messages. |
| Subscription `400` mentioning `criteria`      | A filter is not recognised or not approved for us. Raise it with MNS, quoting the correlation ID. |
| Subscription `409`                            | A clashing subscription already exists. Check with MNS before creating another.                 |
| Subscription `422`                            | The 50 active subscription limit has been reached. Contact MNS.                                 |

## Rollback

`subscribe.cs` does not delete subscriptions. Use `list` to find the subscription ID, then call the
MNS API with a bearer token from the same `/oauth2/token` flow:

```http
DELETE https://int.api.service.nhs.uk/multicast-notification-service/subscriptions/<subscription_id>
Authorization: Bearer <access_token>
X-Correlation-ID: <new uuid>
```

A successful delete returns `204`. Alternatively, ask the MNS team, quoting the subscription ID.

## Production

Follow the same steps with these differences:

- Use `--env prod` (`https://api.service.nhs.uk`) for both `list` and `create`.
- Use the **production** developer portal application and its Key Vault secrets. NHS requires a
  separate application per environment, so the INT credentials do not work in production.
- Use the production MESH mailbox ID.
- Repeat prerequisites 2 and 3 (onboarding approval and adding the MNS API) for the production application.
- Run the dry run first, and have a second person check the mailbox ID and criteria before the real run.
