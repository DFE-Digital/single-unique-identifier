# Authentication and API Edge Strategy

**Date:** `2026-03-24`  
**Owner:** SUI Service Team  
**Scope:** Proposed authentication direction for the active `MATCH` / `FIND` scope, including API edge considerations and environment strategy.

This document captures the current proposed direction for authentication and API edge design in the Single Unique Identifier (SUI) project.

It is intentionally **not** an Architecture Decision Record (ADR). It is a design note intended to:

- formalise the current direction of travel
- support planning and implementation work
- record the main options and trade-offs currently being considered
- provide material that may later inform one or more ADRs

This note should be read alongside [ADR-SUI-0011: Authentication and trust boundaries for SUI APIs](../../Architecture%20decisions/Systems%20landscape/0011-authentication-and-trust-boundaries-for-sui-apis.md), which defines the relevant trust boundaries and baseline machine-to-machine requirements.

Related note:

- [Authentication Baseline and Security Model](./BaselineSecurityModel.md)

---

## 1. Overview

The active service scope is currently centred on `MATCH` and `FIND`, with pull-based polling as the baseline discovery mechanism.

`FETCH` is not the main focus of this work, but it may still need minimal maintenance to preserve existing E2E flows, especially in the Test Harness user interface and E2E tests.

This creates a few important constraints:

- authentication must support both attended and unattended operation
- machine-to-machine access is non-negotiable for key service boundaries
- the system should remain portable across different API edge and issuer choices
- the solution used in Discovery and early Alpha should be close enough to a realistic production shape to generate useful learning

The purpose of this note is to describe a practical direction that meets those constraints without assuming that all platform decisions are already closed.

---

## 2. Current Position

The current working position is:

- the active service focus is `MATCH` and `FIND`
- `FETCH` is not the main focus, but may need minimal maintenance to preserve existing E2E flows
- polling remains the baseline discovery mechanism
- organisation-level authentication is the preferred starting point if policy allows
- direct user authentication to the system remains an open question
- deployed `dev` and `test` environments should resemble production where practical, but do not all need to be identical
- manual onboarding may be acceptable for Alpha if it is sufficient to support useful learning

This note does not assume that the final long-term API edge or issuer decision has already been made.

---

## 3. Proposed Direction

The current proposed direction is to move toward a production-shaped, organisation-level authentication model for `MATCH` and `FIND` while keeping the implementation portable enough to support more than one platform option.

In practical terms, this means:

- use `OAuth 2.0 client_credentials` for machine-to-machine access
- use bearer `JWT` access tokens
- keep the services issuer-agnostic and avoid baking provider-specific assumptions into business logic
- derive a canonical organisation identity from token claims
- use scopes or roles to determine what actions the caller is allowed to perform
- keep direct user authentication to the system out of the baseline until a clearer requirement exists

This direction is intended to work whether the long-term route ends up being:

- an Azure-hosted edge using `Azure API Management (APIM)` and an issuer such as `Entra`, or
- a future [Find and Use an API (FaUAPI)](https://find-and-use-an-api.education.gov.uk/documentation)-based route, if that becomes the agreed approach

---

## 4. Organisation Identity and Roles

An organisation may act as:

- a searcher
- a custodian
- or both

For this reason, the design should not assume separate top-level organisation identities for search and custodian behaviour.

Instead:

- the service should map the incoming token to a canonical `organisation_id`
- scopes or roles should determine whether that organisation is acting as a searcher, custodian, or both
- internal authorisation should be based on organisation identity plus permissions, rather than on gateway-specific identifiers

This avoids duplication in organisation data and keeps the model flexible if user-level assertions are introduced later.

---

## 5. Token and Credential Model

The current proposed token model is:

- external machine clients use `OAuth 2.0 client_credentials`
- access tokens are bearer `JWT`s
- the token carries the claims needed to derive:
  - `organisation_id`
  - granted scopes or roles
  - issuer and audience validity

For external integrations, the likely near-term practical credential type is:

- `client secret`

This should not be treated as a permanent constraint. The design should keep room for stronger client authentication methods such as certificates later if required.

---

## 6. Defence in Depth

The preferred model is defence in depth.

This means:

- the API gateway, where present, should validate tokens and apply coarse protections early
- the application should still validate tokens and enforce business-level authorisation itself

The intention is to avoid a design where the backend trusts the edge blindly.

This approach gives:

- better resilience if gateway configuration drifts
- better portability across different gateway setups
- safer local, CI, and non-production arrangements
- clearer ownership of business authorisation within the services

---

## 7. API Edge and Environment Strategy

Not every environment needs to prove the same thing.

### 7.1 Shared High-Fidelity Environment

At least one shared environment should be close to the likely Azure-hosted operating model.

**Current likely shape:**

- `APIM + standards-compliant issuer`

If available and allowed by governance, an issuer such as `Entra` is the most obvious starting point in Azure-hosted environments. However, the services should not depend on Entra-specific token handling in order to remain portable.

This environment is primarily for learning:

- onboarding and operational implications
- the gateway pattern
- the issuer and token model
- a realistic non-prod representation of the likely deployed shape

### 7.2 Cheaper Dev/Test Environments

Not every dev/test environment necessarily needs full production-style API management.

Possible shapes include:

- `APIM Developer` plus issuer
- `APIM Consumption` plus issuer
- `Front Door` plus issuer, with JWT validation in the application
- direct service access with JWT validation in the application

**These environments primarily exist to support:**

- day-to-day development
- integration testing
- lower-cost non-prod setup

### 7.3 Local / CI / Ephemeral Environments

These environments should preserve the same token contract, but can use lighter-weight infrastructure.

Possible shapes include:

- local or test issuer plus application-level JWT validation
- local or self-hosted reverse proxy or gateway where that improves test coverage

**The aim is to:**

- support fast local development
- support repeatable automated integration and E2E tests
- avoid auth bypasses becoming part of the architecture

---

## 8. Options and Trade-Offs

### 8.1 `APIM + issuer`

**Strengths:**

- closest fit to the likely Azure-hosted gateway model
- good fit for token validation, throttling, policy enforcement, and gateway learning
- least likely to drift too far from a future `FaUAPI`-based route

**Limitations:**

- more expensive than simpler edge options
- may be heavier than needed for every non-prod environment

### 8.2 `Front Door + issuer`

**Strengths:**

- lower-cost edge option
- useful for ingress, routing, TLS termination, and WAF-style protections
- viable in environments where JWT validation remains in the application

**Limitations:**

- not a full API-management gateway
- lower fidelity if the main learning goal is APIM or `FaUAPI`-style edge behaviour

### 8.3 `APIM Developer` or `APIM Consumption`

**Strengths:**

- potentially a better cost/fidelity compromise than full production-style APIM everywhere
- still supports learning the APIM model

**Limitations:**

- still brings APIM cost and complexity
- `APIM Developer` is a non-production compromise and should not be treated as equivalent to a production-grade setup
- may not match the eventual production tier exactly

### 8.4 Separate `FaUAPI` Spike

`FaUAPI` refers to the Department for Education (DfE) Find and Use an API platform, which provides API catalogue and subscription capabilities, plus access patterns for DfE APIs.

**Strengths:**

- allows the DfE route to be investigated without coupling the main service path to it too early
- helps isolate DfE-specific assumptions from the core service design

**Limitations:**

- adds parallel work
- may duplicate some setup effort

### 8.5 Self-Hosted Gateway / Proxy for Local or CI

**Strengths:**

- useful for local and ephemeral testing
- can provide higher-fidelity auth and routing tests than direct service access alone

**Limitations:**

- can drift from the deployed Azure shape if overused
- adds another moving part to maintain

---

## 9. Current Recommendation

The current recommendation is:

- use `APIM`-shaped gateway behaviour in at least one shared, high-fidelity environment
- avoid requiring full APIM in every dev/test environment if the cost is not justified
- keep application-level JWT validation as a baseline so the services remain portable across different gateway choices
- treat `Front Door` as a cost-saving edge option, not as a full replacement for API-management learning
- investigate the `FaUAPI` route separately rather than baking it directly into the main service path too early

At this stage, the first follow-on work should be defining the auth baseline and security model in more detail, since that will shape the rest of the implementation work.

---

## 10. Open Questions and Current Mitigations

Several important questions remain open.

### 10.1 Long-term API edge route

**Open question:**

- will the system ultimately be exposed through `FaUAPI`, a project-managed edge, or another DfE-owned route?

**Current mitigation:**

- keep the service and token handling standards-based
- keep the application issuer-agnostic
- use an `APIM`-shaped model in at least one high-fidelity environment to reduce drift if `FaUAPI` becomes the preferred route
- run a separate `FaUAPI` spike rather than coupling the core service path to assumptions too early

### 10.2 Organisation-only auth versus user-aware auth

**Open question:**

- is organisation-level authentication sufficient for Alpha, or will any operations require asserted end-user identity?

**Current mitigation:**

- design the baseline around organisation-level machine authentication
- derive a canonical organisation identity from claims
- keep room in the auth context for later user assertions if they become necessary

### 10.3 Onboarding model for Alpha

**Open question:**

- what level of onboarding capability is needed in Alpha beyond manual registration and credential issuance?

**Current mitigation:**

- assume manual onboarding is acceptable as the starting point
- avoid making a self-service portal a dependency for the initial auth baseline
- treat Alpha onboarding as something to learn from rather than something that must already be final

### 10.4 Claims and scopes model

**Open question:**

- what exact claims or scopes model is acceptable from a governance and policy point of view?

**Current mitigation:**

- define a minimal baseline model first
- keep claims focused on organisation identity and granted permissions
- avoid coupling the service to provider-specific claim shapes where possible

### 10.5 What survives from the current prototype

**Open question:**

- what parts of the current prototype implementation are intended to survive into Alpha hardening?

**Current mitigation:**

- avoid assuming the current implementation will be carried forward unchanged
- focus near-term work on patterns and contracts that remain useful even if parts of the prototype are later replaced

### 10.6 Practical issuer choice under governance constraints

**Open question:**

- what issuer setup will be practically available within DfE governance constraints?

**Current mitigation:**

- treat `OAuth 2.0` / `JWT` as the primary contract rather than a specific issuer
- use `Entra` as a practical first choice where available
- keep open the possibility of another standards-compliant issuer if governance or platform constraints require it

---

## 11. Suggested Next Work

The broad next areas of work are:

- define the current auth baseline and security model for the active `MATCH` and `FIND` boundaries
- prepare the active services for a generic `OAuth2/JWT` auth model rather than continuing with prototype-only auth paths
- define how auth should work across local development, CI, ephemeral environments, and deployed non-prod environments
- run a separate `FaUAPI` / DfE-hosting spike so that route can be explored without coupling the main service path to it too early
- define the minimum onboarding, credential, audit, and operational governance model needed for Alpha

---

## 12. Relationship to Later ADR Work

This document is intended to support later ADR work. It does not replace ADRs and does not itself record a final architectural decision.

As decisions become firmer, the material here is expected to inform:

- the completion of ADR-SUI-0011
- future API edge decisions
- and any later decision records covering issuer choice, onboarding, or environment strategy
