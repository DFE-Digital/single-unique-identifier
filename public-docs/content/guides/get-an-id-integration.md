---
layout: sub-navigation
title: Integrate with Get an Identifier
includeInBreadcrumbs: true
sectionKey: Developer documentation
eleventyNavigation:
  key: Get an Identifier integration
  parent: Developer documentation
  order: 20
---

This guide is for teams integrating a system with Get an Identifier through the Find and Use API (FaUAPI). It describes the request and response contract implemented by Get an Identifier. FaUAPI supplies the consumer-facing API address and onboarding details for each environment.

## What the API does

Get an Identifier is the vendor-facing API for children's social care in the Single Unique Identifier programme. A caller submits demographic details and the service searches NHS England's Personal Demographics Service (PDS). A confident match returns the person's NHS number and the GP practice ODS code when PDS provides one.

## Before you start

Get access through the approved FaUAPI onboarding process. Before connecting, obtain the values and instructions for your approved environment:

- the FaUAPI API base URL;
- whether the subscription requires an API key and how to send it;
- the OAuth token endpoint, client credential process and scope to request;
- the published API contract for that environment; and
- the approved test environment and test data.

These gateway and onboarding values are environment-specific. This guide does not define them. The Get an Identifier application requires a valid bearer token and the `get-an-identifier.read` permission; use the FaUAPI onboarding instructions for the token and subscription requirements presented to consumers. Do not use a local development address or a backend host as the vendor API address.

## Request flow

1. Obtain an access token and any subscription credential using the approved FaUAPI instructions for your environment.
2. Send a `POST` request to the Get an Identifier operation using the path `/api/v1/get-an-identifier` under the base URL supplied by FaUAPI. The request must include `Content-Type: application/json`, a bearer token in the `Authorization` header, and the required JSON body. Include any FaUAPI subscription key header if your approved onboarding instructions require one.
3. Handle the HTTP status and response body. A `200` response contains a confident match. A `404` means the supplied details did not confidently match a person; it is not a successful match.
4. Keep credentials and demographic data protected throughout processing, storage and logging.

## Request body

The JSON body has one top-level field:

| Field | JSON type | Requirement | Notes |
| --- | --- | --- | --- |
| `personSpecification` | object | Required | Contains the person details listed below. |

### `personSpecification` fields

The person fields and overall model are based on the [Social Care Interoperability Standards (SCIS) PUB01 Person Standard](https://standard.socialcaredata.io/PUB01_person_standard). Get an Identifier does not implement the full standard. Use the fields and validation rules below for this API. Optional fields may be omitted, but supplied values must be valid.

| Field | JSON type | Requirement | Notes |
| --- | --- | --- | --- |
| `given` | string | Required | Given names, including middle names where known. |
| `family` | string | Required | Family name. |
| `birthDate` | string | Required | Date in `YYYY-MM-DD` format. |
| `gender` | string | Optional | If supplied, use `male`, `female`, `unknown` or `other`. |
| `addressPostalCode` | string | Optional | If supplied, it must be a valid UK postcode. |
| `phone` | string | Optional | If supplied, it must be a valid UK telephone number. |
| `email` | string | Optional | If supplied, it must be a valid email address. |

Example request (replace the placeholders with synthetic data approved for the test environment):

```sh
curl --request POST "$GETANID_BASE_URL/api/v1/get-an-identifier" \
  --header "Authorization: Bearer $ACCESS_TOKEN" \
  --header "Content-Type: application/json" \
  --data @request.json
```

Set `GETANID_BASE_URL` to the FaUAPI base URL supplied for your environment and obtain `ACCESS_TOKEN` using its approved authentication instructions. Add a subscription key header only if FaUAPI onboarding tells you to do so, using the header name it specifies.

```json
{
  "personSpecification": {
    "given": "<synthetic given names>",
    "family": "<synthetic family name>",
    "birthDate": "<YYYY-MM-DD>",
    "gender": "unknown",
    "addressPostalCode": "<synthetic UK postcode>"
  }
}
```

Do not copy real people's details into examples, development fixtures or support requests. Use only synthetic test data where the environment owner has confirmed that it is suitable.

## Response handling

A `200 OK` response has this shape:

```json
{
  "personId": "<matched NHS number>",
  "generalPractitioner": [
    "<GP practice ODS code, when supplied by PDS>"
  ]
}
```

`generalPractitioner` can be an empty array when PDS does not provide a registered practice. Get an Identifier does not enrich the ODS code with practice details.

| Field | JSON type | Notes |
| --- | --- | --- |
| `personId` | string | Matched NHS number. |
| `generalPractitioner` | array of strings | GP practice ODS codes supplied by PDS; may be empty. |

Error responses use a JSON problem object with `type`, `title`, `status`, `detail` and `instance` fields. Handle the HTTP status as well as the body; do not assume that a human-readable `detail` value is stable enough for program logic.

| Field | JSON type | Notes |
| --- | --- | --- |
| `type` | string | Problem type identifier. |
| `title` | string | Short problem summary. |
| `status` | integer | HTTP status code. |
| `detail` | string or null | Additional problem detail, when provided. |
| `instance` | string or null | Problem instance identifier, when provided. |

| Status | Meaning |
| --- | --- |
| `400 Bad Request` | The request body is missing, malformed or contains invalid or missing required data. |
| `401 Unauthorized` | Bearer authentication is missing or invalid. |
| `403 Forbidden` | The caller does not have the required permission. |
| `404 Not Found` | The supplied demographic details did not confidently match a person. |
| `502 Bad Gateway` | PDS returned an error or timed out. |
| `500 Internal Server Error` | An unexpected server error occurred. |

FaUAPI may apply its own gateway responses and policies. Follow the error contract and operational guidance supplied with your subscription for gateway-level errors, rate limits and retry behaviour; this API contract does not define them.

## Test the integration

Use only the environment and synthetic test data provided or approved through your onboarding. The repository's post-deployment smoke checks exercise an unauthenticated request (expecting `401`) and an authenticated synthetic request that reaches PDS (which can return `200` or `404`). These are service checks, not a guarantee that any particular vendor test environment or test record is available.

Before production use, verify with the environment owner which test results are expected, how to report an issue and whether any additional gateway checks apply. Do not test with live people's details unless your organisation has explicitly authorised that use and supplied an approved process.

## Security and operational expectations

- Send requests only over HTTPS to the FaUAPI address supplied for your environment.
- Store OAuth client credentials, access tokens and any subscription key in an approved secrets manager. Do not put them in source control, logs, URLs or support tickets.
- Treat the request body, NHS number and GP practice information as sensitive personal data. Limit access and retention to what your organisation needs for the authorised purpose.
- Do not log demographic request or response bodies, NHS numbers, tokens, subscription keys or upstream PDS diagnostics. Use non-sensitive operational information such as HTTP status and approved correlation identifiers when investigating failures.
- Do not cache responses. The application returns `Cache-Control: no-store` headers.
- Apply the retry and rate-limit policy provided for your FaUAPI subscription. This guide does not specify gateway limits or automatic retry rules.
