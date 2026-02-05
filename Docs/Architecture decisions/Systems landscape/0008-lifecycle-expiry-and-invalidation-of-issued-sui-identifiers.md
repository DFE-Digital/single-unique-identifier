# ADR-SUI-0008: Lifecycle, Expiry and Invalidation of Issued SUI Identifiers

<<<<<<< HEAD
Date: 11 December 2025
=======
Date: 2025-12-11
>>>>>>> main

Author: Simon Parsons

Decision owners: SUI Service Team

Category: Identity and lifecycle management

## Status
Superseded by ADR-SUI-0013

> **Superseded**
>
> This ADR was written when custodian-scoped encrypted identifiers and deterministic
> cryptographic derivation were assumed as the primary identity model.
>
> The SUI architecture has since pivoted to using **NHS number in the clear** as the
> Single Unique Identifier. While the underlying lifecycle problem (expiry, invalidation,
> refresh, and coordination) remains relevant, the assumptions and mechanisms described
> in this ADR are no longer current.
>
> See **ADR-SUI-0013 — Lifecycle Management of Shared SUI Identifiers** for the
> current decision and ongoing work.

---

## Context

The SUI service issues **deterministic identifiers** to participating organisations for use in local storage, lookup, and discovery workflows.

Each identifier is generated as a pure function of:

```
ID = F(Organisation, SUI, Epoch)
```

Where:

- **Organisation** is the custodian receiving the identifier
- **SUI** is the subject identifier (for example NHS number), which is not stored centrally
- **Epoch** represents the active cryptographic key epoch
- **F** is a deterministic encryption and encoding function

Determinism is a core architectural requirement because:

- the service must be able to derive the identifier that a given organisation has stored,
- the same organisation must always receive the same identifier for the same subject within an epoch,
- and cross-custodian correlation must remain possible via the central service without storing the SUI at rest.

At the same time, the platform must support real operational lifecycle requirements, including:

- expiry of identifiers after a policy-defined lifetime,
- invalidation of identifiers issued before a subject-level correction or merge,
- cryptographic key rotation via epochs,
- and strict avoidance of randomness or time-based data inside the identifier itself.

These requirements introduce tension between **determinism** and **lifecycle control**, which this ADR addresses.

---

## Problem

Two lifecycle behaviours require the service to reason about **when an identifier was first issued**.

### Time-based expiry

Policies such as:

> “Identifiers older than N months must be refreshed”

require the system to determine the age of an identifier.

### Correction-based invalidation

Where a subject’s SUI is corrected, merged, or superseded, policy may require:

> “Invalidate any identifier issued before time T for this subject”

Again, the service must be able to determine whether an identifier predates or postdates the correction event.

### Constraint: identifiers must remain deterministic

Identifiers must not embed:

- issuance timestamps,
- TTLs,
- randomness,
- or correction markers.

Embedding any time- or event-dependent data would cause the same `{Organisation, SUI, Epoch}` input to yield different identifiers at different times, breaking deterministic regeneration and cross-organisation lookup.

The service therefore requires lifecycle knowledge **without** encoding lifecycle state into the identifier itself.

---

## Alternatives considered

### Encode issuance time or TTL into the identifier  
Rejected because it destroys determinism and prevents the service from reconstructing identifiers already stored by organisations.

### Frequent epoch rotation  
Rejected because epochs become a crude proxy for issuance time, requiring multi-epoch lookups, causing operational instability, and leading to mass expiry events.

### Versioned or corrected SUIs  
Rejected as the primary mechanism because they do not provide issuance time, cannot distinguish “before” vs “after” correction, and still require multi-version lookup.

### Full central registry of identifier → SUI mappings  
Rejected because it creates a centralised subject database, reintroduces SUI at rest, and eliminates the privacy benefits of deterministic identifiers.

---

## Decision

The service will maintain a **minimal central issuance store** to support identifier lifecycle management.

The issuance store will contain exactly:

```
Id, IssuedAt
```

And nothing else.

Specifically:

- **No SUI**
- **No organisation identifier**
- **No epoch**
- **No subject or domain metadata**

This store records the first time a given identifier was generated and enables lifecycle decisions without altering the identifier itself.

---

## Rationale

Without knowing when an identifier was first issued, the service cannot reliably:

- expire identifiers after a fixed lifetime,
- invalidate identifiers issued before a correction event,
- perform safe “next-touch” refresh,
- coordinate rolling cryptographic migrations,
- or ensure consistent behaviour across custodians.

Storing `IssuedAt` separately:

- preserves determinism, as it is not part of identifier generation,
- avoids central storage of the SUI,
- introduces the minimum possible central state to support required behaviour,
- and keeps identifier semantics stable across the ecosystem.

This approach represents the smallest viable compromise between privacy, determinism, and operational reality.

---

## Consequences

### Positive

- Deterministic identifier generation is preserved.
- No SUI is stored centrally.
- Supports age-based expiry policies.
- Supports correction-based invalidation.
- Enables clean epoch transitions.
- Small, privacy-safe state footprint.
- Can be implemented as a high-performance key-value store.

### Trade-offs

- Introduces minimal central state.
- Requires the issuance store to be durable and available.
- Requires a defined retention and cleanup policy.

---

## Implementation notes

- The issuance store is keyed by the **opaque identifier string** exactly as stored by organisations.
- `IssuedAt` is recorded only the first time an identifier is generated.
- Subsequent regeneration under the same `{Organisation, SUI, Epoch}` does not update `IssuedAt`.
- Deleting an entry removes lifecycle knowledge for that identifier.

---

## Out of scope

- Policy rules defining expiry durations.
- Correction detection and subject-level reconciliation logic.
- Audit and event logging.
- Custodian-side refresh mechanics.