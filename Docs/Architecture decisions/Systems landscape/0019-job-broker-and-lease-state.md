# ADR-SUI-0019: Job Broker and Lease State

Date: 17 February 2026  
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Distributed discovery architecture (custodian integration)

---

## Status

Proposed

---

## Executive Summary

ADR-SUI-0018 defines how custodians poll FIND and claim work under a time-bound lease. This ADR defines the minimum infrastructure and storage model required to make that lease model correct, fast, and easy to operate during Alpha.

The Alpha baseline uses **Azure Table Storage as the single source of truth** for job state and lease state. The design intentionally avoids background sweepers and avoids write-heavy indexing. The aim is to keep Table Storage interactions minimal:

- **No-work poll:** one partition-scoped query returning `204`  
- **Successful claim:** one query + one conditional update  
- **Completion:** one conditional update  

Redis is **not** part of the Alpha baseline. It is explicitly deferred as an optional optimisation to suppress idle reads if telemetry proves it is needed.

This ADR covers broker/storage only. It does not redefine endpoint semantics (see ADR-SUI-0018).

---

## 1. Context

FIND implements a pull-based discovery model. Custodians poll FIND to claim work, perform local lookup, and submit results. The dominant cost driver in polling systems is usually the idle case (many polls returning “no work”). Therefore the Alpha design must keep both of these true:

1. **Correctness:** only one custodian can hold a lease for a given job at any moment.  
2. **Efficiency:** maintaining lease state must not be onerous; Table transactions must be minimised and access patterns must avoid scans.

This ADR assumes enterprise deployments behind proxies and load balancers, and standard HTTP client stacks.

---

## 2. Non-Negotiable Requirements

### 2.1 Core correctness rule

At any moment, a single job must be actively assigned (leased) to no more than one custodian.  

It must not be possible for two custodians to process the same job at the same time.

Where multiple custodians are required to carry out the same task, the task shall be represented by multiple independent jobs.

Competing claim attempts for the same job must result in exactly one successful claim and all other attempts being rejected in a deterministic manner.


### 2.2 Minimal state maintenance

The design must not depend on background maintenance processes to keep job state correct.

Specifically:

- When a lease expires, the job must automatically become eligible for re-claim based solely on its stored timestamps. No background task should be required to reset flags or update state.
- In the common “no work available” case, polling must require only a single read operation.
- Write operations must occur only when meaningful progress happens (job creation, successful claim, completion, or explicit lease renewal if enabled).

The system must avoid periodic sweepers, cleanup daemons, or repair jobs as part of normal operation.


### 2.3 Partition-scoped access patterns

When a custodian requests work, the system must be able to locate eligible jobs by querying only that custodian’s partition in Table Storage.

The design must avoid scanning the entire table across all custodians.

If one custodian generates significantly more traffic than others, the design must allow explicit scaling of partitions for that custodian. Any such scaling approach must be deliberate, observable, and operationally controlled.

---

## 3. Decision

### 3.1 Alpha baseline decision

FIND SHALL store durable job metadata and lease state in **Azure Table Storage** using a single authoritative table (`Jobs`).

Leasing SHALL be represented as **lease overlay fields** (id/expiry). The lifecycle SHALL be represented using timestamps rather than state transitions.

All claim/complete transitions SHALL be enforced using optimistic concurrency (ETag + `If-Match`). Where two claimers race, one update succeeds and the other fails deterministically (`412 Precondition Failed`).

Azure Table Storage assigns an `ETag` to every entity version. When a job is read, its current `ETag` is returned. Claim and completion updates are performed using `If-Match` with that `ETag`, meaning the update will only succeed if the entity has not been modified since it was read. If another claimant updates the entity first, the `ETag` changes and the second update fails with `412 Precondition Failed`. This provides deterministic lease exclusivity without distributed locks.

Jobs older than a rolling 72 hour window SHALL be ignored for polling selection. Reinsertion or escalation of such jobs is treated as a separate operational concern outside this ADR.

### 3.2 Deferred optimisation

Redis MAY be introduced later only to suppress idle reads and reduce claim latency, but it SHALL remain non-authoritative and fully disposable. Correctness SHALL never depend on Redis.

---

## 4. Why Azure Table Storage for Alpha

Azure Table Storage is selected because it supports the Alpha requirements with minimal operational overhead:

- **Deterministic exclusivity without locks:** optimistic concurrency provides a clean “winner/loser” claim outcome under contention, avoiding distributed locks.  
- **Horizontal scalability aligned to access pattern:** Table Storage scales by partition. Because work is partitioned by custodian and polling is partition-scoped, the workload naturally aligns with the storage model and can scale out predictably.  
- **Cost model aligned to Alpha learning:** Table Storage is consumption-based and transaction-priced, with no database engine to provision or maintain. This keeps fixed costs low while allowing throughput to scale with demand.  
- **Low operational burden:** no database engine to patch/tune; capacity planning is simpler during Alpha learning.  
- **Entity-centric model fit:** the job and lease are naturally modelled as a single entity with atomic transitions.  
- **Minimal write amplification:** the baseline requires no additional index tables and no background maintenance tasks.

Table Storage is not chosen because it is universally best; it is chosen because it is sufficient for Alpha while keeping the architecture simple, economical, and defendable.

---

## 5. Why not Service Bus or a relational queue table for Alpha

### 5.1 Broker-first (Service Bus)

A broker provides built-in visibility locks and settlement semantics, which is attractive for competing consumers. However, it typically still requires a separate durable store for rich job metadata, correlation, attempts, and lease lifecycle diagnostics. That yields a two-system design at MVP and couples behaviour to broker semantics rather than a service-owned lease model.

This remains a viable future direction, but is not required to satisfy Alpha correctness.

### 5.2 Relational queue table (Postgres/SQL)

Relational queues provide strong transactional semantics and mature patterns. The trade-off is operational overhead and the need to manage contention, connection pooling, and database health. Alpha does not yet justify paying that cost unless telemetry forces it.

---

## 6. Storage Model

### 6.1 Table: `Jobs` (authoritative)

One entity per job.

#### Keys

- `PartitionKey`: `custodianId`  
- `RowKey`: `{CreatedAtUtcTicks:D20}|{JobId}`

The RowKey embeds a monotonic time prefix to support stable “oldest-first” selection within a partition.

The RowKey begins with a fixed-width UTC ticks prefix. Because Azure Table Storage performs lexicographic comparisons on RowKey values, range queries using the ticks prefix (for example `RowKey >= "{windowStartTicks:D20}|"`) remain fully constrained by time. The `{JobId}` suffix does not affect the ability to bound scans by time; it exists only to guarantee uniqueness and stable ordering within identical tick values.

#### Properties

| Property | Type | Meaning |
|---|---|---|
| JobId | string | The id of the job |
| SearchId | string | Business correlation identifier |
| LeaseId | string? | Current lease identifier |
| LeaseExpiresAtUtc | datetime? | Lease end time |
| AttemptCount | int | Number of times the job has been claimed |
| CreatedAtUtc | datetime | Creation timestamp |
| UpdatedAtUtc | datetime | Last update timestamp |
| CompletedAtUtc | datetime? | When the job was confirmed as completed |
| PayloadType | string | The job type |
| PayloadJson | string | Payload JSON |
| JobTraceParent | string? | Stored trace context seed |

#### Core idea: visibility is time-based

A job is claimable when:

- `CompletedAtUtc` is null
- AND `AttemptCount <= maxAttempts`
- AND (`LeaseExpiresAtUtc` is null OR `LeaseExpiresAtUtc < now`)

Jobs older than 72 hours are out of scope for polling selection. In Table Storage this is enforced by restricting the query to RowKeys within the 72 hour window (because RowKey is prefixed with creation ticks).

### 6.2 Table: `FindJobResults`

Results are stored separately to optimise aggregation by job.

#### Keys

- `PartitionKey`: `JobId`  
- `RowKey`: `{custodianId}|{SubmittedAtUtcTicks:D20}`

#### Properties

| Property | Type | Meaning |
|---|---|---|
| SearchId | string | Business correlation identifier |
| LeaseId | string | Lease under which result was submitted |
| SubmittedAtUtc | datetime | Submission time |
| ResultJson | string | Result payload |
| CustodianCorrelationId | string? | Custodian diagnostic correlation |
| ResultTraceParent | string? | Inbound trace context if supplied |

---

## 7. Access Patterns and Transaction Counts

### 7.1 Enqueue a job (progress event)

**Transactions:** 1 write

Insert `Jobs` entity:

- `CreatedAtUtc = now`
- `AttemptCount = 0`
- Lease fields null

### 7.2 Poll / claim a job (progress event)

**Idle case (no work)**  
**Transactions:** 1 query

Query within `custodianId` partition:

- Filter: `RowKey >= windowStartTicks`
- Order: by RowKey (natural)
- Page until first eligible entity found

If none found: respond `204`.

**Successful claim**  
**Transactions:** 1 query + 1 conditional update

For the selected entity, attempt conditional update (`If-Match` ETag):

- `LeaseId = new GUID`
- `LeaseExpiresAtUtc = now + leaseDuration`
- `AttemptCount = AttemptCount + 1`
- `UpdatedAtUtc = now`

If conditional update fails (`412`), continue scanning for next eligible candidate (bounded by policy defined in ADR-SUI-0018).

### 7.3 Complete a job (progress event)

**Transactions:** 1 conditional update

- `CompletedAtUtc = now`
- `UpdatedAtUtc = now`

### 7.4 Renew a lease (optional)

**Transactions:** 1 conditional update

- Verify `LeaseId` matches
- Extend `LeaseExpiresAtUtc`

If renew is not implemented, the job becomes eligible again after expiry (within the 72 hour window).

---

## 8. Partitioning Strategy

Azure Table throughput is partition-bound. The baseline uses one partition per custodian (`PartitionKey = custodianId`).

Further partitioning MAY be introduced later if telemetry indicates sustained partition throttling.

---

## 9. Backpressure, Idle Efficiency, and Why Redis is Deferred

In polling systems, the primary lever for idle efficiency is not storage choice; it is client behaviour (backoff) and server backpressure.

Alpha SHALL rely on:

- `204` responses when no work exists  
- `429` / `503` with `Retry-After` when throttling is required  
- mandatory client jitter/backoff (defined in ADR-SUI-0018)

Redis is deferred because introducing it by default increases operational surface area. If telemetry later shows that idle Table queries are too expensive, Redis can be introduced as a read-suppressing hint layer.

Even then, Table remains authoritative for claims.

---

## 10. Failure Modes

**Lease expiry**  
No action required. Eligibility returns automatically when `LeaseExpiresAtUtc < now` (subject to the 72 hour window).

**Concurrent claim attempts**  
Resolved deterministically by conditional update (`412` on loser). Correctness preserved.

**Stale jobs beyond window**  
Jobs older than 72 hours are not considered for polling. Operational reinsertion is external to this ADR.

**Table throttling**  
Surfaced through `429`/`503` and client backoff.

**Redis failure (if introduced later)**  
No correctness impact; system falls back to Table-only behaviour.

---

## 11. Comparative Summary

| Option | Correctness | Idle Efficiency | Operational Complexity | Fit for Alpha |
|---|---|---|---|---|
| Table-only (windowed selection) | ✅ Strong | ⚠️ Medium | ✅ Low | Recommended baseline |
| Table + Redis hints | ✅ Strong | ✅ High | ⚠️ Medium–High | Introduce only if needed |
| Service Bus + metadata store | ✅ Strong | ✅ High | ⚠️ Medium | Viable alternative model |
| Postgres/SQL queue table | ✅ Strong | ✅ High | ⚠️ Medium | Viable but heavier |

---

## 12. Consequences

### Positive

The Alpha baseline is simple, deterministic, and cheap to operate. Lease expiry requires no background maintenance, and Table transactions remain tightly bounded.

### Trade-offs

Performance is shaped by partition distribution and window size. Jobs older than 72 hours require separate operational handling if still required.

---

## 13. Diagram

```mermaid
flowchart TB
  Cust["Custodian outbound HTTPS"] --> API["FIND Polling API"]
  API --> Jobs["Azure Table Storage<br/>Jobs<br/>Authoritative job and lease state"]
  API --> Results["Azure Table Storage<br/>FindJobResults<br/>Results by JobId"]
  API -.->|optional| R["Redis<br/>Dispatch hints"]
```
