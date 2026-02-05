# ADR-SUI-0013: Lifecycle Management of Shared SUI Identifiers

<<<<<<< HEAD
Date: February 2026  
=======
Date: 2026-02-05
>>>>>>> main
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Identity lifecycle and coordination

## Status
Draft

---

## Context

The SUI programme uses the NHS number as the Single Unique Identifier (SUI) for
cross-custodian discovery and coordination.

While the identifier itself is stable and issued in the clear, the validity of an issued identifier can change over time.

Identifiers are issued based on:
- a snapshot of demographic data provided by a custodian,
- a point-in-time resolution against authoritative demographic services,
- and an assumption that the resolved NHS number remains valid for that record.

In practice, this assumption does not always hold.

This ADR addresses the need to manage the **lifecycle of issued identifiers** — including
staleness, refresh, invalidation, and reissuance — in a distributed custody environment.

This ADR supersedes **ADR-SUI-0008**, which addressed similar concerns under a deterministic
encryption model.

---

## Why lifecycle management is required

### Demographic drift over time

Custodians submit demographic records that represent a snapshot in time. As time passes,
those records may diverge from authoritative data due to:

- address changes,
- name updates,
- corrections to date of birth,
- gender changes,
- or late-arriving demographic information.

As drift increases, the probability that a future match against authoritative sources
yields a **different NHS number** also increases.

Regular re-matching improves data quality and confidence but requires visibility of how
old issued identifiers are and when refresh should be encouraged or enforced.

---

### Authoritative corrections and reassignments

Authoritative demographic services may emit events indicating that:

- NHS numbers have been corrected,
- demographic records have been merged or split,
- identifiers have been superseded due to life events (for example gender reassignment).

These events may be delivered via **NEMS or MNS**.

When such events occur, identifiers previously issued by the service may no longer be
correct and may require coordinated remediation across all affected custodians.

---

### Custodian and system change

Custodian environments are not static. Over time:

- organisations may merge, split, or reorganise,
- new internal systems may be introduced,
- legacy systems may be retired,
- record identifiers may change,
- record type definitions may evolve.

These changes may require:
- re-issuing identifiers to new systems,
- associating identifiers with additional custodian systems,
- or invalidating relationships that are no longer meaningful.

---

### Service scope and eligibility changes

The service may impose eligibility rules that evolve over time, for example:

- individuals ageing out of scope,
- policy-driven changes to which records are discoverable,
- or changes in what constitutes a valid subject for discovery.

Lifecycle management must support these transitions explicitly rather than relying on
implicit decay or undefined behaviour.

---

### Previously unresolved or ambiguous matches

There will be cases where:
- no confident match was possible at the time of issuance,
- or where an ambiguous match was deferred.

Subsequent updates to authoritative data may allow these records to be resolved later.
The service must be able to recognise and act on these opportunities.

---

## Role of the identity register

As described in **ADR-SUI-0009**, the service maintains a register of observed relationships
between identifiers, custodians, systems, and record types.

For lifecycle purposes, this register (or a closely related store) acts as:

- the **primary index** of who has been issued which identifiers,
- the coordination point for determining affected parties,
- and the anchor for issuing follow-up tasks or notifications.

The register is not required to be an immutable historical ledger, and may be treated as
ephemeral or policy-bounded where appropriate.

---

## Lifecycle actions in scope

This ADR recognises, but does not yet fully specify, the following lifecycle actions:

- **Refresh**  
  Encouraging or requiring custodians to re-match demographic records after a defined
  period or confidence decay.

- **Invalidation**  
  Marking previously issued identifiers as no longer valid due to authoritative correction
  or policy change.

- **Reissuance**  
  Issuing updated identifiers following correction, system change, or successful re-match.

- **Expiry / forgetting**  
  Allowing the service to forget issuance or awareness after a defined period to reduce
  stale coordination state.

Each of these actions may require **coordinated communication** with one or more custodians.

---

## Event-driven triggers

Lifecycle actions may be triggered by:

- authoritative demographic events (for example via NEMS or MNS),
- policy-defined staleness thresholds,
- custodian-initiated signals (for example system change),
- or service-initiated quality controls.

This ADR deliberately does not define the mechanics of event consumption or task delivery.
Those concerns are addressed in related ADRs.

---

## Relationship to discovery and task orchestration

As described in **ADR-SUI-0012**, the service uses a pull-based task model to coordinate
discovery across custodians.

Lifecycle actions such as refresh, invalidation, and reissuance naturally fit the same
coordination pattern and may be expressed as additional task types alongside discovery.

This unifies operational behaviour and avoids introducing parallel integration models.

---

## Decision

The service will treat issued identifiers as **lifecycle-managed artefacts** rather than
permanent facts.

The service will maintain sufficient state to:
- assess the age and validity of issued identifiers,
- determine which custodians are affected by lifecycle events,
- and coordinate appropriate follow-up actions.

The specific mechanisms, policies, and thresholds remain to be defined.

---

## Consequences

### Positive
- Improves long-term correctness and data quality.
- Enables response to authoritative correction events.
- Supports distributed custody without centralising record content.
- Aligns lifecycle management with existing task-based coordination.

### Trade-offs
- Introduces ongoing operational responsibility for lifecycle tracking.
- Requires careful governance to avoid unnecessary churn.
- Defers detailed policy and implementation decisions to future work.

---

## Related ADRs

- ADR-SUI-0009 — Central Custodian Knowledge Register  
- ADR-SUI-0010 — Use of NHS Number as the Single Unique Identifier  
- ADR-SUI-0012 — Asynchronous Pull-Based Discovery for FIND  
- ADR-SUI-0008 — Lifecycle, Expiry and Invalidation of Issued SUI Identifiers (superseded)
