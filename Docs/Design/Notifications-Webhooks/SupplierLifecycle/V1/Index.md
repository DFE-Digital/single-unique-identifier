# Supplier lifecycle webhook contract v1

**Contract version:** `1`

**Status:** Draft

**Owner:** SUI Service Team

This document defines the HTTP webhook contract that suppliers implement to receive lifecycle notifications from the SUI Notification Service. A notification means that information previously returned by Get an Identifier may have changed. It does not contain the replacement information.

The machine-readable contract consists of:

- [OpenAPI 3.1 webhook definition](./OpenApi.yml)
- [JSON Schema Draft 2020-12 payload schema](./LifecycleNotification.schema.json)

The contract remains in Draft until the acknowledgement timeout is agreed and the technical and information-governance reviews are complete.

## Supplier processing requirement

Suppliers must:

1. authenticate and validate the complete request;
2. durably enqueue the event, or durably identify it as a duplicate;
3. return an empty `202 Accepted` response within the agreed acknowledgement timeout; and
4. rematch potentially affected records asynchronously through Get an Identifier.

Returning `202 Accepted` before the notification is durably accepted is not compliant. Suppliers must not delay the response while rematching or updating their records.

The proposed acknowledgement timeout is **three seconds**. This is a working proposal, not an agreed SLA. The final value must be agreed with the technical team and suppliers and updated here before this contract moves to Accepted.

## Request

The Notification Service sends an HTTPS `POST` to the supplier's registered endpoint with this media type:

```http
Content-Type: application/vnd.dfe.sui.lifecycle-notification.v1+json
```

The endpoint URL is supplier-specific and does not carry the contract version. The media type and `schemaVersion` field identify v1.

### Payload

Every payload has the same envelope:

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | string | The major payload contract version. It is `1` for this contract. |
| `eventId` | UUID | Stable identifier for the normalized lifecycle event and the supplier idempotency key. |
| `eventType` | string | `nhsNumberChanged` or `gpChanged`. |
| `occurredAt` | RFC 3339 date-time | UTC timestamp associated with the normalized lifecycle event. |
| `affectedNhsNumber` | string | Unformatted, checksum-valid 10-digit NHS number used to locate potentially affected records. |

The NHS number must contain exactly ten digits, must not start with zero and must pass the NHS modulus-11 checksum. JSON Schema validators that do not implement the custom `nhs-number` format will enforce only the structural digit pattern; suppliers must also enforce the checksum rule.

The payload is deliberately strict. It never contains:

- the new NHS number;
- updated GP details;
- demographic information; or
- the raw MNS or FHIR event.

Unknown properties are invalid. Any new property or event type requires a new major contract version and media type.

### NHS number changed example

For `nhsNumberChanged`, `affectedNhsNumber` is the **old NHS number** because that is the value suppliers currently hold. The new NHS number is obtained only by rematching.

```json
{
  "schemaVersion": "1",
  "eventId": "27dd8c17-e05d-432f-a14f-6b05d1f7469b",
  "eventType": "nhsNumberChanged",
  "occurredAt": "2026-09-02T09:30:00Z",
  "affectedNhsNumber": "9876543210"
}
```

### GP changed example

For `gpChanged`, `affectedNhsNumber` is the person's **current NHS number**. Updated GP details are obtained only by rematching.

```json
{
  "schemaVersion": "1",
  "eventId": "8ab24d4d-b1cd-4516-92d8-c854f066af59",
  "eventType": "gpChanged",
  "occurredAt": "2026-09-02T09:35:00Z",
  "affectedNhsNumber": "9434765919"
}
```

The NHS numbers in these examples are synthetic test values.

## Delivery headers

Every request includes:

| Header | Meaning | Retry behaviour |
|---|---|---|
| `X-SUI-Delivery-ID` | Identifies delivery of the event to this registered endpoint. | Stable across retries to this endpoint. |
| `X-SUI-Delivery-Attempt` | One-based delivery attempt number. | Incremented for each retry. |
| `X-SUI-Timestamp` | Unix time in whole seconds at which the attempt was signed. | Regenerated for each attempt. |
| `X-SUI-Key-ID` | Identifies the endpoint-specific signing secret used. | Changes only during secret rotation. |
| `X-SUI-Signature-256` | HMAC-SHA256 signature. | Regenerated for each attempt. |
| `X-Correlation-ID` | Operational tracing identifier; not an idempotency key. | May be reused to correlate attempts for the same source flow. |

An `eventId` identifies one normalized lifecycle change, not the raw source message. If one PDS change produces both supported change types, the MNS integration must produce two distinct, repeatable event IDs. Redelivery of the same source change must reproduce the same normalized event ID.

## HMAC-SHA256 verification

Each registered endpoint has its own cryptographically random signing secret. It is provisioned as standard padded Base64 representing at least 32 random bytes (256 bits). Both parties must Base64-decode the provisioned value and use the resulting bytes directly as the HMAC key. They must not use the UTF-8 bytes of the Base64 text or interpret that text as hexadecimal.

The secret is exchanged outside the webhook request and is never included in payloads, URLs, logs or traces.

The Notification Service constructs the signed bytes as follows:

```text
ASCII(X-SUI-Timestamp)
+ "."
+ ASCII(X-SUI-Delivery-ID)
+ "."
+ exact raw request-body bytes
```

It then calculates:

```text
X-SUI-Signature-256 =
    "sha256=" + lowercase_hex(HMAC-SHA256(signing-secret, signed-bytes))
```

Suppliers must:

1. read the raw body bytes before JSON parsing or reserialization;
2. resolve `X-SUI-Key-ID` to an active or grace-period signing secret;
3. reject timestamps more than five minutes before or after the supplier's current time;
4. calculate the signature using the exact raw bytes;
5. compare the supplied and calculated signatures using a constant-time comparison;
6. validate the media type and JSON payload before acknowledging it.

Signature failure, an unknown or inactive key, or a timestamp outside the replay window returns `401 Unauthorized`. Malformed delivery metadata returns `400 Bad Request`.

Endpoint secret rotation must allow a controlled overlap in which current and next key IDs can both be verified. Compromised keys must be revocable without changing the endpoint URL.

### Signing test vector

This vector is for interoperability testing only. The secret is deliberately public and must never be used outside tests.

```text
provisioned secret (Base64): MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=
decoded key bytes (ASCII):   0123456789abcdef0123456789abcdef
timestamp:                   1788341400
delivery ID:                 96e7275d-5fa4-4b1b-b978-21779e03dbd4
```

The exact body bytes are the following single UTF-8 line with no trailing newline:

```json
{"schemaVersion":"1","eventId":"27dd8c17-e05d-432f-a14f-6b05d1f7469b","eventType":"nhsNumberChanged","occurredAt":"2026-09-02T09:30:00Z","affectedNhsNumber":"9876543210"}
```

The expected header is:

```text
X-SUI-Signature-256: sha256=703b385e5ad09d1971a40a7a9868ae090d3fb0a77233d0a2fc702785e7c90fa8
```

## Acknowledgement and idempotency

Only an empty `202 Accepted` response is a successful acknowledgement. Other `2xx` responses are contract failures. A successful response means that the supplier has durably accepted the event; it does not mean that rematching has completed.

Delivery is at least once and ordering is not guaranteed. Suppliers must:

- use `eventId`, not `deliveryId` or `X-Correlation-ID`, as the idempotency key;
- retain processed event IDs for at least 30 days;
- return `202 Accepted` for a previously accepted event without enqueueing or rematching it again; and
- handle later events independently rather than assuming that they arrive in occurrence order.

The 30-day deduplication period is aligned to MESH operating limits rather than chosen arbitrarily. MESH makes an uncollected mailbox message unavailable after five days, but NHS England can manually resend it within 30 days of the original send. See the [NHS England MESH client reference guide](https://digital.nhs.uk/developer/api-catalogue/message-exchange-for-social-care-and-health-api/mesh-client/mesh-client---reference-guide).

## Response and retry classification

The Notification Service uses these classifications:

| Outcome | Classification |
|---|---|
| Empty `202 Accepted` | Acknowledged; do not retry. |
| DNS, connection, TLS or other transport failure | Retryable. |
| Acknowledgement timeout | Retryable. |
| `408 Request Timeout` | Retryable. |
| `425 Too Early` | Retryable. |
| `429 Too Many Requests` | Retryable. |
| Any `5xx` response | Retryable. |
| Any `3xx` response | Non-retryable; redirects are not followed. |
| Any other `2xx` response | Non-retryable contract failure. |
| Any other `4xx` response | Non-retryable contract or endpoint-configuration failure. |

The Notification Service honours a valid `Retry-After` response on `429` or `503` where the delivery mechanism permits it. Suppliers must not rely on a particular retry interval or number of attempts. The retry schedule, permanent-failure handling and the effect of aggregate supplier delivery results on MESH acknowledgement are implementation concerns outside this contract.

## Normalized internal event boundary

The MNS and webhook workstreams must agree an internal event that supplies the webhook contract with:

| Value | Requirement |
|---|---|
| Event ID | Stable per normalized lifecycle change and repeatable across source redelivery. |
| Event type | `nhsNumberChanged` or `gpChanged`. |
| Occurred at | UTC timestamp associated with the normalized change. |
| Affected NHS number | Old NHS number for NHS-number changes; current NHS number for GP changes. |

The delivery component creates the endpoint-specific delivery ID, attempt number, signing timestamp and signature. It must not expose the raw MNS/FHIR event or enrich the payload with replacement information.

## Information governance and review

The Notification Service broadcasts every normalized lifecycle notification to every enabled supplier endpoint. This means a supplier can receive an NHS number even when it holds no record for that person.

The information-governance review must explicitly consider:

- the broadcast of an NHS number to every enabled supplier endpoint;
- the purpose limitation that the identifier is used only to locate potentially affected records and initiate rematching;
- the authorization and assurance required before an endpoint is enabled;
- the exclusion of replacement and demographic data from the payload;
- supplier storage, access, retention and deletion expectations; and
- prevention of NHS numbers, payload bodies and signing secrets appearing in logs or traces.

This document moves from Draft to Accepted only after the technical and information-governance reviews are complete, the normalized event contract is agreed and the acknowledgement timeout is finalized.
