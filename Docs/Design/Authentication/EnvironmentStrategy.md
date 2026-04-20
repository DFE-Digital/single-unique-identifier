# Authentication Environment Strategy

**Date:** `2026-04-14`  
**Owner:** SUI Service Team  
**Scope:** Target authentication environment strategy for local development, CI, ephemeral environments, cheaper shared non-prod environments, and at least one shared high-fidelity environment for the active inbound auth scope.

This document builds on [Authentication and API Edge Strategy](./Index.md), [Authentication Baseline and Security Model](./BaselineSecurityModel.md), and [ADR-SUI-0011: Authentication and trust boundaries for SUI APIs](../../Architecture%20decisions/Systems%20landscape/0011-authentication-and-trust-boundaries-for-sui-apis.md).

It defines how the auth baseline should be exercised across environments without relying on auth bypasses as a substitute for realistic testing.

---

## 1. Purpose

The purpose of this note is to make the environment strategy concrete enough to support:

- implementation planning for local, CI, and deployed non-prod auth
- consistent auth behaviour across fast feedback and higher-fidelity environments
- clear decisions on where `APIM` is required and where it is not
- realistic testing using real tokens, scopes, and seeded identities instead of auth bypasses

This note is an environment strategy, not a final production decision record.

---

## 2. Design Drivers

The environment strategy is built around the following drivers:

- the external auth contract should stay recognisably the same across environments
- local and CI flows must stay fast and self-contained
- at least one shared environment must exercise the likely Azure-hosted auth shape
- cheaper shared environments should optimise for cost and regression coverage, not full edge fidelity
- the services must remain issuer-agnostic and must not depend on provider-specific claim handling
- tests that claim to cover auth should obtain and use real bearer tokens rather than bypass middleware or endpoint checks

For the active scope, the stable cross-environment contract is:

- `OAuth 2.0 client_credentials`
- bearer `JWT` access tokens
- scope-based API permissions
- application-level authorisation using canonical organisation identity plus permissions

---

## 3. Current Starting Point

The repo currently behaves as follows:

- local development and ephemeral CI run the `Find` and `StubCustodians` services directly, without `APIM`
- the `Find` API issues sandbox inbound tokens at `/api/v1/auth/token` using `Data/auth-clients-inbound.json`
- the stub custodians issue sandbox outbound tokens at `/api/v1/auth/token` using `auth-clients-outbound.json`
- `Find` validates bearer JWTs in application middleware today; there is no implemented gateway-trust mode yet
- shared `d01` and `d02` environments currently use direct Function App and Web App URLs, not an `APIM` front door
- shared auth and organisation fixture data now lives in the top-level `Data/` directory and is copied into app outputs

This is already better than an auth bypass because tests obtain signed bearer tokens and hit protected endpoints. However, it is still a prototype environment shape rather than the full target operating model.

Two current limitations matter for this strategy:

- the shared hosted environments do not yet exercise `APIM` or an external issuer
- the seeded inbound auth clients currently all carry the full scope set, which is useful for happy-path testing but weaker than the desired role-separated test model

---

## 4. Target Environment Shapes

### 4.1 Environment matrix

| Environment | Target edge shape | Target issuer shape | Validation and enforcement path | Seeded data approach | Primary purpose |
| --- | --- | --- | --- | --- | --- |
| Local development | Direct service access by default; optional local reverse proxy only for targeted gateway tests | Repo-managed sandbox issuer | `Find` validates JWTs in-app; application enforces scopes, ownership, and policy hooks | Repo-managed synthetic fixtures, including org directory and auth clients | Fast development and debugging |
| CI ephemeral | Direct self-contained service startup; no `APIM` dependency | Same sandbox issuer contract as local | Same in-app JWT validation and endpoint protection as local | Same synthetic fixtures, resettable on every run | Repeatable automated regression and E2E testing |
| Cheaper shared non-prod | Direct service access by default; optional low-cost ingress later, but no `APIM` requirement | Environment-owned sandbox or test issuer preserving the same JWT contract | Application-level JWT validation remains authoritative | Same synthetic org and client catalogue, but shared-environment secrets should come from environment-managed secret storage | Hosted integration testing and lower-cost shared regression |
| Shared high-fidelity | `APIM` as mandatory ingress | Standards-compliant shared issuer, preferably `Entra` where governance allows | `APIM` performs coarse token checks; application still enforces permissions, ownership, and policy, and should continue JWT validation until a trusted gateway-forwarded auth path exists | Same synthetic participants may still be used for learning, but onboarding, credentials, and signing material should be environment-managed | Operational learning on issuer, gateway, onboarding, and auth support model |

### 4.2 Local development

The local target shape is:

- direct access to locally running services
- sandbox token issuance through the same auth endpoints the system already exposes for prototype flows
- application-level JWT validation in `Find`
- no local auth bypass flags for protected API operations

An optional local proxy or gateway can be used for targeted experiments, but it should not become a required dependency for day-to-day development.

### 4.3 CI and ephemeral environments

The CI and ephemeral target shape is intentionally close to local development:

- start the services directly in the job
- use Azurite and other local dependencies as needed
- obtain bearer tokens from a sandbox issuer
- run against real protected endpoints

This keeps auth coverage realistic while avoiding dependence on shared infrastructure for every PR.

### 4.4 Cheaper shared non-prod environments

The cheaper shared non-prod target shape is:

- keep direct hosted service access as the default
- do not require `APIM` in every shared dev/test environment
- keep JWT validation and scope enforcement inside the application
- preserve the same token and scope contract used locally and in CI

Current `d01` and `d02` fit this bucket more naturally than the high-fidelity bucket. They are useful for hosted regression and integration testing, but they are not yet sufficient for learning the final issuer and gateway operating model.

### 4.5 Shared high-fidelity environment

At least one shared environment should be designated high-fidelity for auth learning.

The target shape for that environment is:

- `APIM` in front of the SUI APIs
- a standards-compliant shared issuer such as `Entra`, subject to governance availability
- realistic routing, token validation, diagnostics, and operational ownership
- no dependence on repo-managed static signing keys or repo-managed shared non-prod secrets

This environment exists to learn the parts that local and cheaper non-prod cannot prove well:

- issuer integration
- gateway policy and coarse token enforcement
- operational onboarding and credential lifecycle
- realistic support and troubleshooting paths

---

## 5. Expected Role of APIM

The expected role of `APIM` is environment-specific:

- local development: not required
- CI and ephemeral environments: not required
- cheaper shared non-prod: optional, but not the default or blocking dependency
- shared high-fidelity: required

Where `APIM` is present, its baseline responsibilities should be:

- token signature validation
- issuer validation
- audience validation
- token lifetime checks
- coarse scope gating where practical
- rate limiting, routing, and edge observability

`APIM` should not become the only place where authorisation logic lives. The application still owns:

- auth-context construction
- searcher versus custodian endpoint separation
- organisation-to-resource ownership checks
- policy and DSA enforcement inputs

---

## 6. Issuer Strategy by Environment

### 6.1 Local, CI, and ephemeral

These environments should use a repo-managed sandbox issuer shape.

That means:

- synthetic clients and scopes are defined in repo fixtures
- token issuance remains self-contained and fast
- tests can request realistic bearer tokens without depending on a shared tenant or manual setup

This is acceptable here because these environments are disposable and exist to support rapid feedback.

### 6.2 Cheaper shared non-prod

Cheaper shared non-prod should preserve the same token contract, but it should move away from repo-managed shared secrets over time.

The recommended target is:

- keep the sandbox or test-issuer model initially if that is the lowest-friction route
- keep the services issuer-agnostic
- move shared-environment signing material and client secrets into environment-managed secret storage rather than keeping them as repo-shipped shared non-prod secrets

This allows the environment to stay cheap while still being more credible than a pure local-only setup.

### 6.3 High-fidelity

The high-fidelity environment should use a shared standards-compliant issuer.

The preferred first choice is:

- `Entra`, where governance and platform access make that practical

If `Entra` is not practically available, the fallback is another standards-compliant issuer, provided the token contract still satisfies the auth baseline.

The service should continue treating `OAuth 2.0` and `JWT` as the real contract, not any one issuer brand.

---

## 7. Seeded Data and Test Data Strategy

### 7.1 Shared fixture model

Auth testing should use a small canonical synthetic fixture set that is stable across environments.

That fixture set should cover at least:

- searcher-only clients
- custodian-only clients
- a dual-role organisation where needed
- disabled or insufficient-scope clients for negative tests
- stable synthetic organisation IDs used consistently across auth and policy fixtures

### 7.2 Where seeded data should live

The recommended model is:

- local, CI, and ephemeral environments can continue using repo-managed synthetic auth data
- cheaper shared non-prod and high-fidelity environments should keep the same non-secret fixture identities and scope model
- shared-environment secrets, signing keys, and client secrets should come from environment-managed secret storage or issuer configuration rather than from repo-shipped shared secrets

This means the fixture model should stay portable, but secret material should not.

### 7.3 What counts as an auth bypass

The following do **not** count as unacceptable auth bypasses for this strategy:

- using a sandbox issuer that still issues signed bearer tokens
- using seeded synthetic clients and scopes
- running direct-to-service auth flows in local or CI without `APIM`

The following **would** count as unacceptable auth bypasses for realistic auth testing:

- disabling JWT validation for test environments
- anonymous access to endpoints that are meant to require bearer tokens
- test-only code paths that skip scope or ownership enforcement

---

## 8. Minimum Environment Needed for Useful Learning

The minimum viable auth environment set is:

- self-contained local and CI environments using the sandbox issuer and real JWT validation
- one shared high-fidelity environment using `APIM` and a shared standards-compliant issuer

If only one shared environment can be justified for auth learning, that shared environment should be the high-fidelity one.

Cheaper shared non-prod environments are still useful, but they are primarily for hosted regression and team convenience, not for learning the final edge and issuer operating model.

---

## 9. Cost and Fidelity Trade-Offs

| Shape | Strengths | Limitations |
| --- | --- | --- |
| Local / CI sandbox issuer with direct service access | Cheapest, fastest, self-contained, good for repeatable auth regression | Does not prove `APIM`, external issuer integration, or shared operational model |
| Cheaper shared non-prod without `APIM` | Good hosted regression coverage at lower cost, aligns closely with the current repo setup | Lower edge fidelity and weaker learning on issuer and gateway operations |
| Shared high-fidelity with `APIM` and shared issuer | Best learning on gateway, issuer, onboarding, observability, and support model | Highest cost and operational overhead |

The recommended overall trade-off is:

- keep local and CI deliberately lightweight
- keep cheaper shared non-prod pragmatic and low-cost
- spend the auth fidelity budget in one shared environment that actually teaches the team something about the likely deployed shape

---

## 10. Current Recommendation

The current recommended target is:

- local development uses direct service access, sandbox token issuance, and application-level JWT validation
- CI and ephemeral environments use the same auth contract and avoid test-only bypass paths
- current shared `d01` and `d02` environments continue to act as cheaper shared non-prod environments, not as the primary high-fidelity auth environment
- at least one shared high-fidelity environment should be introduced or designated for `APIM` plus shared-issuer learning
- seeded auth data should be tightened to model searcher-only, custodian-only, dual-role, and negative-test clients more realistically
- shared-environment secret material should move out of repo-managed shared fixtures and into environment-managed secret storage or issuer-managed configuration

---

## 11. Immediate Implementation Implications

If this strategy is adopted, the main implementation implications are:

- keep the current local and ephemeral CI auth flows standards-shaped rather than replacing them with bypasses
- treat `d01` and `d02` as cheaper hosted regression environments unless and until one is deliberately upgraded to high-fidelity auth
- split seeded auth metadata from secret material so shared environments can stop relying on repo-managed shared secrets
- add role-separated seeded auth clients so auth tests can exercise realistic permission boundaries
- plan follow-on work for an `APIM` plus shared-issuer high-fidelity environment rather than trying to force that shape into every non-prod environment

---

## 12. Relationship to Later Work

This note is intended to support:

- implementation hardening of the current auth baseline
- later environment and onboarding decisions
- `APIM` and shared-issuer rollout planning
- later ADR work covering issuer choice, gateway rollout, and non-prod auth operations
