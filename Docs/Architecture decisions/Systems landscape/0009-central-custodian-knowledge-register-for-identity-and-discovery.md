# ADR-SUI-0009: Central Custodian Knowledge Register for Identity and Discovery

Date: 20 January 2026

Author: Simon Parsons

Decision owners: SUI Service Team

Category: Systems landscape

## Status

Accepted

---

## Context

The SUI service is testing two architectural designs that must be supported by the same register:

1) **Managed ID Register**  
   The SUI service maintains a register of which custodians have held which record types for a person (identified by SUI). Discovery is primarily served by querying this register.

2) **Distributed Id**  
   Custodians are expected to store an identifier provided by the service. Discovery may involve **fan-out** queries to custodians to confirm which record types they hold for a person. In this design, the register is still required to track **freshness** and to identify custodians who may need to rotate or refresh the identifiers they store.

### Meaning of the register
A row means:

> **A custodian held a record of the given type for the person at some point.**

It does **not** guarantee the custodian currently holds the record.

### Inputs and workflows
The discovery workflow may start with:
- a **demographic** (requiring a PDS match to obtain NHS number and derive SUI), or
- a **preknown id** (which may already be the SUI).

The service also supports **fan-out discovery** (Distributed Id design): the service queries custodians and receives confirmations of record types held for a person (identified by SUI/NHS number).

### Data to store (facts only)
The register must store facts that have been observed, including:
- custodian identifier
- system identifier (software product)
- record type
- SUI (derived from demographic via PDS match, or provided as preknown SUI)
- custodian’s subject identifier (may match SUI)
- first time the service became aware the custodian held the record (“first seen”)
- last time the relationship was observed (“last seen”) to support freshness
- provenance of the id relationship (whether the id was issued by the service, already known by the custodian, or discovered via other means)
- when the service last delivered an id to that custodian for this relationship (Distributed Id design)

A non-reversible footprint/signature of demographics is explicitly out of scope for this ADR and may be added later as a separate decision.

---

## Decision

Implement a **current-state register** in **Azure Table Storage** (or Cosmos DB Table API) to support low-latency discovery and efficient writes.

The register will be updated using **idempotent upserts** and will support:
- immediate discovery by querying all rows for a given SUI (Managed ID Register and Distributed Id),
- freshness monitoring and notifications using timestamps (Distributed Id and operational needs),
- learning new custodian relationships from fan-out confirmations even where no prior issuance record exists.

Audit/event logging may be implemented separately and asynchronously; it is not part of the critical path for this ADR.

---

## Rationale

- The discovery path requires a fast, predictable lookup of “which custodians have held records for this person?” The natural access pattern is **partition by SUI**.
- A current-state register supports immediate reads without requiring projections or event replay.
- Table Storage provides cost-effective, high-throughput upserts and partitioned queries.
- Derived states (for example “stale” or “current”) are computed from timestamps and policy rules; they are not stored as mutable status fields.

---

## Data model

### Table: `SuiCustodianRegister`

#### Partitioning
The table is partitioned by SUI so that discovery for a person is a single-partition query.

#### Keys
- **PartitionKey**: `SUI_{Sui}`
- **RowKey**: `C_{CustodianId}|RT_{RecordType}|SYS_{SystemId}`

This key ensures that a custodian can have distinct rows per record type and system identifier without overwriting.

#### Properties
All properties are factual observations; no inferred status is stored.

**Identity**
- `Sui` (string)
- `CustodianId` (string)
- `RecordType` (string)
- `SystemId` (string)
- `CustodianSubjectId` (string, nullable)

**Observed times**
- `FirstSeenUtc` (DateTimeOffset)  
  The first time the service became aware the custodian held the record type for this SUI (at some point).
- `LastSeenUtc` (DateTimeOffset)  
  The most recent time the service observed evidence relevant to this relationship (for example custodian refresh/obtain activity, or custodian confirmation via fan-out).

**Provenance**
- `Provenance` (string)  
  A controlled vocabulary describing how the relationship became known. Minimum required values:
  - `IssuedByService`
  - `AlreadyHeldByCustodian`
  - `DiscoveredViaFanout`

**Distributed Id issuance tracking**
- `LastIdDeliveredUtc` (DateTimeOffset, nullable)  
  The most recent time the service delivered an identifier to this custodian for this SUI/recordType/system relationship.

---

## Query patterns

### Q1: Discovery by SUI (primary)
Input: `Sui`  
Operation: query `SuiCustodianRegister` by `PartitionKey = SUI_{Sui}` and return all rows.

### Q2: Discovery starting from demographic
1. Resolve NHS number via PDS match.
2. Derive SUI.
3. Run Q1.

### Q3: Discovery starting from preknown id
- If the preknown id is the **SUI**, run Q1 immediately.
- If the preknown id is **not** the SUI, the service cannot query this register until it can obtain the SUI.

### Q4: Freshness / rotation candidates (derived)
Freshness is derived from:
- `LastSeenUtc`
- `LastIdDeliveredUtc`
- policy thresholds (defined outside this ADR)

---

## Write flows

### Flow A: Custodian obtain/refresh
1. Resolve SUI.
2. Upsert register row.
3. Set `FirstSeenUtc` if new.
4. Always update `LastSeenUtc`.
5. If id delivered, set `LastIdDeliveredUtc`.

### Flow B: Search
Resolve SUI if required, then query by partition key.

### Flow C: Fan-out discovery confirmation
When a custodian confirms they hold a record type:
- Insert/update the row.
- Set `FirstSeenUtc` if new.
- Update `LastSeenUtc`.
- Set `Provenance = DiscoveredViaFanout`.
- Leave `LastIdDeliveredUtc` null unless an id was delivered.

---

## Consequences

### Positive
- One register supports both architectures.
- Fast discovery.
- Accurate freshness signals.
- Correct handling of fan-out discovery without prior issuance.

### Trade-offs
- Table Storage limits global secondary queries.
- “Held at some point” semantics may include custodians who no longer hold records.

---

## Out of scope
- Audit/event logs
- Global staleness indexing
- Demographic footprints
- Mapping for non-SUI preknown ids
