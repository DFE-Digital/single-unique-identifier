# ADR-SUI-0013: Lifecycle Management of Shared SUI Identifiers

Date: TBC  
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Identity and lifecycle management

## Status
Draft

---

## Context

The SUI architecture has evolved to use **NHS number in the clear** as the Single Unique Identifier (SUI), replacing earlier assumptions based on custodian-scoped encrypted identifiers.

Despite this change, the platform must still support **lifecycle concerns** such as expiry, invalidation, refresh, and coordination across custodians, independently of the identifier format.

This ADR is intended to capture the **current and future decision** on how identifier lifecycle is managed in the post-encryption architecture.

This ADR is expected to **supersede ADR-SUI-0008** once finalised.

---

## Notes (working)

- This ADR exists to re-anchor **expiry and invalidation** in a world where:
  - the identifier is stable (NHS number),
  - lifecycle semantics are policy-driven,
  - and lifecycle state must not be encoded into the identifier itself.
- The previous ADR (0008) addressed the same problem under deterministic encryption assumptions.
- Open questions to return to:
  - What minimal central state (if any) is required for lifecycle reasoning?
  - How are expiry and invalidation enforced in FIND vs FETCH?
  - How are custodians notified or coordinated when lifecycle events occur?
  - How does lifecycle interact with discovery freshness and correction events?
- This ADR should be revisited once:
  - the NHS-number-as-SUI position is fully locked,
  - and the operational model for correction, refresh, and invalidation is clearer.

---

## Decision
TBD

---

## Consequences
TBD

---

## Related decisions
- ADR-SUI-0008 — Lifecycle, Expiry and Invalidation of Issued SUI Identifiers (to be superseded)
- ADR-SUI-0010 — Use of NHS Number as the Single Unique Identifier
- ADR-SUI-0012 — Asynchronous Pull-Based Discovery for FIND
