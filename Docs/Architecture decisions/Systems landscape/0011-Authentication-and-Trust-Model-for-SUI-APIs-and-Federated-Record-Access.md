# ADR-SUI-0011: Authentication and Trust Model for SUI APIs and Federated Record Access

Date: January 2026 (draft)  
Author: Simon Parsons  
Decision owners: SUI Service Team (with input from DfE / participating custodians)  
Category: Security architecture

## Status
Draft — discovery in progress (no decision yet)

---

## Context

The SUI service provides national discovery capabilities across multiple sovereign data controllers (custodians). Searchers (consuming organisations/services) use SUI to:

- **MATCH**: resolve a person and obtain the platform identifier used for discovery (e.g. NHS number/SUI as decided elsewhere).
- **FIND**: discover which custodians may hold relevant record types for a person.
- **FETCH**: obtain access to underlying record content (directly from a custodian system, via an intermediary, or via a portal).

Authentication and trust are a critical architectural axis because they determine:

- who trusts whom and at what boundary,
- which parties can call which endpoints,
- whether SUI remains discovery-only or becomes an enforcement boundary,
- whether record access is federated or centralised,
- and the onboarding and operational cost for participants.

This ADR defines the authentication and trust model for:
- Searcher → SUI (MATCH / FIND)
- Custodian → SUI (registration, status/health, optional callbacks)
- SUI → Custodian (fan-out discovery calls, manifest retrieval)
- Searcher → Custodian (direct record access, where applicable)

This ADR is intentionally scoped to **authentication** and **trust relationships**. Authorisation and policy enforcement are handled by a separate ADR.

---

## Why this ADR is still open

At the current stage, we do not yet have sufficient clarity on:
- whether MATCH/FIND will ever require knowledge of the **end user** (not just the calling organisation),
- which identity provider / trust framework DfE intends to mandate (if any),
- whether custodians will accept organisation-only credentials for discovery and/or retrieval,
- and whether the long-term FETCH pattern requires token exchange / brokered trust to reduce bespoke integrations.

As a result, this ADR remains a working document used to frame decisions and capture constraints. The intention is to converge during Alpha as DfE and custodian onboarding requirements become clearer.

---

## Goals

- Establish a clear, adoptable trust model that supports multi-custodian participation.
- Support secure, scalable machine-to-machine usage for MATCH/FIND where appropriate.
- Avoid premature commitment to a single FETCH pattern before required.
- Enable incremental onboarding with minimal bespoke security work per participant.
- Maintain auditability and revocability of access credentials.
- Preserve the ability to introduce end-user context later if required (without re-architecture).

---

## Non-goals

- Defining fine-grained authorisation rules (handled in ADR-SUI-00XX: Authorisation & Purpose Binding).
- Defining the detailed fetch mechanism (handled in ADR-SUI-00XX: Record Access Pattern / Fetch Architecture).
- Defining encryption/key management for identifiers (handled in ADR-SUI-00XX: Identifier Protection / Crypto).
- Defining the DSA/legal basis per use case (handled in governance artefacts).

---

## Key terms and actors

- **Searcher**: an organisation/service attempting to discover and retrieve records for a person.
- **Custodian**: an organisation/system that holds record content and remains responsible for its disclosure decisions.
- **SUI service**: the national discovery service providing MATCH and FIND, and optional orchestration.
- **Interactive user**: a human user authenticating via a UI (e.g. portal/CMS).
- **M2M**: machine-to-machine authentication (headless service calls).

---

## Decision
Not yet decided.

This ADR will capture the decision once we have confirmed:
- DfE identity/trust expectations (IdP, token model, certificate requirements),
- whether end-user identity must be conveyed to SUI and/or custodians,
- and which FETCH patterns are feasible for a representative set of custodians.

---

## Decision drivers

| Driver | Why it matters |
|-------|-----------------|
| Adoption friction | Must be achievable for a wide range of custodians/searchers without major re-platforming. |
| Security & revocation | Compromise must be containable and credentials revocable quickly. |
| Federation reality | Some custodians will require direct authentication for access to content. |
| Operational cost | Credential issuance/rotation/revocation must be sustainable. |
| Auditability | Calls must be attributable to an organisation/system and potentially a user context. |
| Performance | MATCH/FIND must remain low-latency and scalable. |
| Governance alignment | Must align with DfE direction and cross-government expectations. |

---

## Current working assumptions (subject to change)

These are deliberately framed as assumptions rather than constraints.

- MATCH/FIND are likely to be **organisation-authenticated** at minimum (M2M).
- It is increasingly likely we will need to support **end-user context**, either:
  - directly (user-authenticated calls to SUI), or
  - indirectly (user context asserted by the Searcher and carried through to custodians).
- FETCH is likely to remain **federated**, with custodians retaining an authentication boundary for content access.

---

## Constraints and assumptions to confirm with DfE

- Primary identity provider / platform (DfE-managed, cross-gov, NHS, or none mandated).
- Whether mTLS is mandated, optional, or infeasible for some participants.
- Whether MATCH/FIND will ever be called directly by interactive users.
- Whether SUI will ever proxy record content (data processing implications).
- Whether custodians must support headless APIs for content, or may require portal access.

---

## Options considered

This ADR considers trust models at two layers:
1) **Core discovery calls (MATCH/FIND)** — typically M2M.
2) **Record access (FETCH)** — may be federated and may be M2M or interactive.

### Option A: Centralised M2M for MATCH/FIND; federated FETCH (custodian boundary)
- Searchers authenticate to SUI for MATCH/FIND using M2M credentials.
- Custodians authenticate to SUI for operational integration and fan-out.
- SUI returns pointers and/or tokens directing searchers to custodians for FETCH.
- Custodians enforce authentication for FETCH.

### Option B: Central proxy model (SUI mediates FETCH)
- Searchers authenticate only to SUI.
- SUI calls custodians and proxies record content back to searchers.
- Custodians authenticate SUI, not individual searchers.
- Shifts enforcement boundary and data-processing responsibilities to SUI.

### Option C: Fully federated model (minimal SUI auth; direct custodian access dominates)
- SUI provides discovery only and returns custodian endpoints.
- Searchers authenticate primarily to custodians.
- SUI authentication is minimal (registration/rate controls).
- Places integration and operational burden on searchers/custodians.

### Option D: Hybrid with token exchange / brokered trust
- Searchers authenticate to SUI.
- SUI issues a short-lived artefact (token/assertion) that a custodian can validate.
- Custodian performs its own final authentication/authorisation decision.
- Avoids proxying content while reducing bespoke integration complexity.

---

## Comparative analysis (authentication + trust focus)

### Core trust relationships (who authenticates to whom)

| Relationship | Option A | Option B | Option C | Option D |
|-------------|----------|----------|----------|----------|
| Searcher → SUI (MATCH/FIND) | ✅ Strong M2M | ✅ Strong M2M | ⚠️ Reduced role | ✅ Strong M2M |
| SUI → Custodian (fan-out/manifest) | ✅ Required | ✅ Required | ⚠️ Optional | ✅ Required |
| Searcher → Custodian (FETCH) | ✅ Yes | ❌ No | ✅ Yes | ✅ Yes |
| Custodian → SUI (registration/ops) | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

### Strengths and drawbacks (authentication perspective)

| Consideration | Option A | Option B | Option C | Option D |
|--------------|----------|----------|----------|----------|
| Clear M2M story for MATCH/FIND | ✅ | ✅ | ⚠️ | ✅ |
| Supports direct custodian boundary for content | ✅ | ❌ | ✅ | ✅ |
| Minimises SUI exposure to record content | ✅ | ❌ | ✅ | ✅ |
| Simplifies searcher experience | ⚠️ | ✅ | ❌ | ⚠️ |
| Minimises bespoke FETCH integration | ⚠️ | ✅ | ❌ | ✅ |
| Data-processing complexity for SUI | ✅ Lower | ❌ Higher | ✅ Lower | ✅ Lower |
| Revocation granularity | ✅ High | ⚠️ Medium | ✅ High | ✅ High |
| Operational burden on SUI | ✅ Medium | ❌ High | ✅ Low | ⚠️ Medium/High |

---

## Emerging direction (not a decision)

- MATCH/FIND are strong candidates for **M2M authentication** as a baseline.
- It is increasingly likely that we will need to support **end-user attribution**, even if MATCH/FIND remains organisation-authenticated.
- FETCH is likely to require **federation**, because custodians may require searchers to authenticate directly for content access.

---

## Baseline requirements (draft, incomplete)

### For Searchers
- Must authenticate to SUI for MATCH/FIND using: **TBD**  
  - Candidate pattern: OAuth 2.0 client credentials for organisation identity.
- Must support: **TBD**  
  - mTLS: TBD  
  - Key rotation: TBD  
  - Rate limiting and request attribution: TBD

### For Custodians
- Must authenticate calls from SUI using: **TBD**
- Must support discovery/manifest calls addressed by: **TBD** (identifier type to be confirmed)
- For FETCH, custodians may require:  
  - M2M API access, or  
  - interactive login via portal/CMS, or  
  - brokered assertion (token exchange)

### For SUI Service
- Must support credential lifecycle: issuance, rotation, revocation, audit.
- Must enforce rate limits and request attribution.
- Must produce logs suitable for security investigation.
- Must be able to carry end-user context if required (format and trust model TBD).

---

## Open questions for DfE

| Question | Why it matters | Answer |
|---------|-----------------|--------|
| Is MATCH/FIND expected to be M2M only, or will interactive users call SUI directly? | Determines whether SUI needs user auth flows. | TBD |
| Is there an existing DfE trust framework or mandated identity provider? | Determines feasibility and onboarding friction. | TBD |
| Are we permitted to proxy record content through SUI? | Major governance and boundary implications. | TBD |
| Should custodians be allowed to require direct authentication for FETCH? | Determines whether federation is a requirement. | TBD |
| Do we need token exchange / brokered trust? | Determines complexity and interoperability. | TBD |
| Is mTLS required for all participants? | Affects feasibility for diverse organisations. | TBD |
| Are there constraints on bearer tokens, lifetimes, or signing keys? | Impacts operational model. | TBD |
| Do we need end-user identity, or is organisational attribution sufficient? | Determines whether SUI must accept or carry user context. | TBD |

---

## Consequences
Not yet assessed (decision not made).

---

## Next steps to finalise this ADR

1. Confirm DfE direction on required trust framework / identity provider expectations.
2. Confirm whether SUI may ever proxy record content (and in which circumstances).
3. Confirm whether custodians may require direct searcher authentication for FETCH.
4. Confirm whether end-user attribution is required and, if so, how it must be represented.
5. Select the preferred trust model option (A/B/C/D) and record final Decision + Consequences.
6. Create follow-on ADRs:
   - ADR-SUI-00XX: Authorisation & Purpose Binding
   - ADR-SUI-00XX: Record Access Pattern / Fetch Architecture
   - ADR-SUI-00XX: Credential lifecycle and operational controls (rotation, revocation, monitoring)
