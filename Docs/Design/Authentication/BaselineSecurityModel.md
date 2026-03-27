# Authentication Baseline and Security Model

**Date:** `2026-03-26`  
**Owner:** SUI Service Team  
**Scope:** Baseline authentication and security model for the active `MATCH` / `FIND` scope, the custodian polling flow, and the minimum `FETCH` support needed to preserve existing end-to-end flows.

This document builds on [Authentication and API Edge Strategy](./Index.md) and [ADR-SUI-0011: Authentication and trust boundaries for SUI APIs](../../Architecture%20decisions/Systems%20landscape/0011-authentication-and-trust-boundaries-for-sui-apis.md).

It defines the current baseline needed to guide implementation and follow-on design work.

---

## 1. Purpose

The purpose of this note is to make the current authentication direction concrete enough to support:

- implementation planning for the active service path
- a consistent application auth context across the services
- a clear split between gateway responsibilities and service responsibilities
- later refinement of environment, onboarding, and platform decisions

This note is a baseline, not a final end-state design.

---

## 2. Scope and Non-Goals

### 2.1 In scope

- organisation-level authentication for the active `MATCH` and `FIND` boundaries
- organisation-level authentication for custodian polling and work submission endpoints
- the minimum `FETCH` support needed to preserve existing end-to-end flows
- the baseline claims, permissions, and internal auth context needed by the services
- the minimum security checks expected at the gateway and in the application

### 2.2 Out of scope

- the final long-term issuer choice
- the final API edge choice
- the final onboarding portal or administrator experience
- final user-aware authentication or asserted user identity design
- detailed policy enforcement and Data Sharing Agreement enforcement logic
- the long-term `FETCH` model

---

## 3. Baseline Principles

The baseline model is built around the following principles:

- organisation-level machine-to-machine authentication is mandatory at the key service boundaries
- attended end-user authentication remains the responsibility of the consuming organisation and application
- a single organisation may act as a searcher, a custodian, or both
- authentication and policy enforcement are related but separate concerns
- the service should remain portable across more than one issuer or gateway option
- defence in depth should apply, with checks at both the gateway and application layers

This means the service should prove who the calling organisation/application is, what permissions it has, and whether it is allowed to call the operation. It should not treat authentication alone as sufficient for sensitive data disclosure decisions.

```mermaid
sequenceDiagram
    participant App as Calling application
    participant Issuer as Issuer
    participant Gateway as API gateway
    participant Service as SUI service
    participant Policy as Policy enforcement

    App->>Issuer: Request access token
    Issuer-->>App: Bearer JWT

    App->>Gateway: API request + bearer JWT
    Gateway->>Gateway: Validate issuer, audience, and lifetime
    Gateway-->>Service: Forward request

    Service->>Service: Build auth context
    Service->>Service: Check permission and ownership
    Service->>Policy: Evaluate policy where needed
    Policy-->>Service: Allow or deny
    Service-->>App: Response
```

---

## 4. Baseline Boundary Model

### 4.1 End user -> Searcher application

End-user authentication happens in the consuming application and remains outside direct control of the SUI service.

The service baseline should therefore assume:

- a human user may or may not be known to the SUI service
- user authentication and local role-based access control are handled by the consuming organisation
- service-to-service operations must still work after the end user has left the flow

### 4.2 Searcher application -> SUI service

The searcher-side boundary must support unattended operation for:

- `MATCH`
- starting `FIND`
- polling `FIND` status
- retrieving `FIND` results
- minimal `FETCH` support where needed to preserve the current end-to-end path

The baseline at this boundary is:

- organisation-level machine authentication
- bearer access tokens
- operation-level permissions based on scopes or roles
- job and result access checks tied to the calling organisation

### 4.3 Custodian -> SUI service

The custodian-side boundary is always unattended.

The baseline at this boundary is:

- organisation-level machine authentication
- bearer access tokens
- permissions that allow polling, claiming work, and submitting results
- enforcement that a custodian can only act as itself and only on its own work

### 4.4 `FETCH`

`FETCH` is not the main focus of the active work, but the baseline needs to preserve the current end-to-end path.

For now, the baseline assumption is:

- `FETCH` remains minimally supported
- no expansion of the `FETCH` trust model is assumed in this note
- the long-term `FETCH` model remains a separate decision

---

## 5. Baseline Auth Context

The services should not operate directly on raw provider-specific token claims.

Instead, token validation should produce a normalised internal auth context containing at least:

- `organisation_id`
- `client_id`
- `permissions`
- `issuer`
- `subject`

The intent of these fields is:

| Field             | Purpose                                                    |
| ----------------- | ---------------------------------------------------------- |
| `organisation_id` | Canonical internal identifier for the calling organisation |
| `client_id`       | Identity of the calling application or workload            |
| `permissions`     | Normalised scopes or roles granted to the caller           |
| `issuer`          | Issuer identifier for audit and diagnostics                |
| `subject`         | Stable token subject for traceability                      |

The auth context may later grow to include asserted user context, but that is not part of the baseline.

### 5.1 Organisation identity

The baseline requirement is that the service must be able to derive a canonical `organisation_id` from trusted authentication data.

That does **not** require the raw token to contain an `organisation_id` claim in every environment.

The canonical organisation identity may be derived from:

- a dedicated organisation claim, where available, or
- a trusted server-side mapping from validated client identity to organisation identity

This allows the service to stay portable across issuers that expose slightly different claim shapes.

### 5.2 Current implementation gap

The current `Find` implementation uses an auth context that contains only:

- `ClientId`
- `Scopes`

That is useful as a prototype, but it is not sufficient as the longer-term baseline because it does not make canonical organisation identity explicit.

---

## 6. Baseline Permission Model

The current working permission model should stay close to the active implementation unless there is a clear reason to rename it.

This keeps the baseline practical while leaving room to refine naming later.

### 6.1 Searcher-side permissions

| Operation             | Baseline permission |
| --------------------- | ------------------- |
| Match a person        | `match-record.read` |
| Start a search        | `find-record.write` |
| Cancel a search       | `find-record.write` |
| Read search status    | `find-record.read`  |
| Read search results   | `find-record.read`  |
| Minimal fetch support | `fetch-record.read` |

### 6.2 Custodian-side permissions

| Operation                       | Baseline permission |
| ------------------------------- | ------------------- |
| Check whether work is available | `work-item.read`    |
| Claim work                      | `work-item.write`   |
| Submit work item results        | `work-item.write`   |

### 6.3 Notes on permission naming

The current permission names should be treated as a working baseline, not as a final taxonomy.

They may later be renamed if the team decides that a different naming scheme is clearer or better aligned with the final service shape.

---

## 7. Baseline Token Model

The baseline token model is:

- `OAuth 2.0 client_credentials`
- bearer `JWT` access tokens
- short-lived access tokens
- no dependency on end-user presence

The service should be able to validate the following token concerns regardless of issuer:

| Concern         | Baseline expectation                                |
| --------------- | --------------------------------------------------- |
| Issuer          | Trusted and expected                                |
| Audience        | Matches the target service                          |
| Lifetime        | Token is not expired and is valid for current use   |
| Client identity | A stable client/application identity can be derived |
| Permissions     | Required scopes or roles can be derived             |

The service should accept more than one claim shape where that is needed for portability, provided the claims are mapped into the normalised internal auth context.

---

## 8. Enforcement Model

### 8.1 Gateway responsibilities

Where an API gateway is present, the baseline expectation is that it should handle coarse security checks such as:

- token signature validation
- issuer validation
- audience validation
- token lifetime checks
- coarse permission gating where practical
- rate limiting and other edge protections

These checks are helpful, but they are not sufficient on their own.

### 8.2 Application responsibilities

The application should still enforce the security model itself.

At a minimum, the service should:

- validate or trust only already-validated tokens from an expected path
- build the normalised auth context
- check the required permission for the operation
- enforce organisation-to-resource ownership rules
- ensure that searchers cannot use custodian endpoints and custodians cannot use searcher endpoints unless explicitly permitted
- apply policy enforcement separately where the operation requires it
- emit audit and correlation information using the normalised auth context

### 8.3 Resource ownership checks

The baseline model requires explicit ownership checks for resources such as:

- search jobs
- search results
- work items

Examples:

- a searcher should only be able to read or cancel searches owned by its organisation unless a later design explicitly allows otherwise
- a custodian should only be able to claim or complete work that is assigned to that custodian organisation
- requests must not rely on client-supplied organisation identifiers where the caller identity can be derived from the token

---

## 9. Relationship to Policy Enforcement

Authentication and policy enforcement should not be conflated.

The baseline auth model answers questions such as:

- which organisation is calling?
- which application or workload is calling?
- what operations is it allowed to attempt?

Policy enforcement answers different questions such as:

- whether this organisation is entitled to see that result
- whether the requested use is allowed for the current purpose
- whether record existence or content may be disclosed

The auth baseline should therefore feed policy enforcement, not replace it.

---

## 10. Open Points Carried Forward

This baseline deliberately leaves several points open:

- whether user-aware auth is needed for any operations
- whether `organisation_id` should eventually come from a dedicated token claim everywhere
- whether the current permission names should be renamed
- the final issuer and gateway choice
- the long-term `FETCH` model

These points should not block adoption of the baseline unless they directly affect the active implementation path.

---

## 11. Immediate Implementation Implications

If this baseline is adopted, the main implications for implementation are:

- the auth context should evolve beyond `ClientId` and `Scopes`
- canonical organisation identity should become explicit in the application layer
- endpoint permissions should be documented and enforced consistently
- search and work-item ownership checks should be explicit
- token-to-auth-context mapping should be abstract enough to support more than one issuer
- minimal `FETCH` support can remain, but should not drive the baseline design

---

## 12. Relationship to Later Work

This note is intended to support:

- environment strategy work
- implementation hardening work in the active services
- onboarding and governance work
- later architecture decision records, if and when decisions become firm
