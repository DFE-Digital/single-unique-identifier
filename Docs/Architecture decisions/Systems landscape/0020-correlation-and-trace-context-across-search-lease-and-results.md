# ADR-SUI-0020: Correlation and Trace Continuity Across Distributed Search and Polling

Date: 17 February 2026  
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Observability and lifecycle traceability

---

## Status

Proposed

---

## 1. Context

FIND operates as a distributed, asynchronous system.

A single human action — initiating a search — results in:

1. Creation of a Search record.
2. Creation of one or more Jobs (one per custodian).
3. Independent polling by custodians.
4. Lease acquisition by custodians.
5. Result submission by custodians.
6. Aggregation of results back to the Search.

These steps occur across:

- Multiple inbound HTTP requests.
- Multiple outbound HTTP calls.
- Independent storage transactions.
- Potentially long time gaps between events.

Crucially, lease claim and result submission do not occur within the original HTTP request that created the search. They are separate HTTP requests initiated later by custodians.

This breaks naïve distributed tracing assumptions.

If correlation relies purely on request-scoped trace headers (for example W3C `traceparent`), the lifecycle will fragment into unrelated traces.

However, operationally we require the ability to answer questions such as:

- What happened for Search X?
- Which custodians claimed work?
- Did a lease expire?
- Was a result submitted under the correct lease?
- Why did a job retry?

Therefore correlation must be designed deliberately, not left to incidental header propagation.

---

## 2. The Core Problem

Distributed tracing standards such as W3C Trace Context assume:

- A request triggers downstream calls.
- Trace context flows synchronously via headers.
- Child spans remain within the same logical trace tree.

The FIND polling model does not behave this way.

The lifecycle is:

Search → Job persisted → Time passes → Custodian polls → Lease granted → Time passes → Custodian submits result

There is no continuous request chain.

Each interaction is a new HTTP request that may originate from a completely different system at a different time.

Therefore:

- The original traceparent header is not naturally available.
- Custodians may generate their own trace context.
- Some custodians may not propagate trace headers at all.

If we rely solely on incoming headers, trace continuity becomes unreliable.

---

## 3. Correlation Model

The design separates two concepts:

### 3.1 Business correlation (authoritative)

Business identifiers describe the lifecycle of domain entities.

These include:

- `SearchId`
- `JobId`
- `LeaseId`

These identifiers are mandatory, persisted, and authoritative.

They allow deterministic joins between:

- Search creation
- Job creation
- Lease claim
- Result submission

Business correlation does not depend on transport headers.


### 3.2 Trace correlation (transport-level)

Trace correlation refers to W3C `traceparent` identifiers used by observability platforms.

Trace context is useful for:

- Visualising distributed traces in Application Insights.
- Measuring latency across components.
- Debugging infrastructure behaviour.

However, trace context is not authoritative business identity.

Trace context can be missing, regenerated, or altered by intermediaries.

Therefore it cannot be the sole mechanism for lifecycle correlation.

---

## 4. Decision

### 4.1 Search as the lifecycle root

When a search is initiated:

- A `SearchId` SHALL be created (or accepted if supplied by a trusted upstream).
- A trace context SHALL be active for that request.
- The trace identifier for that request SHALL be persisted as part of each Job created.

This persisted value is referred to as the job’s **trace seed**.


### 4.2 Job-level trace seed

Each Job SHALL store:

- `SearchId`
- `JobTraceParent` (or equivalent trace identifier)

The purpose of storing `JobTraceParent` is to allow later asynchronous operations to reattach to the original logical trace.

This enables lease claims and result submissions — which are separate HTTP requests — to appear within the same trace tree in Application Insights.


### 4.3 Lease claim handling

When a custodian successfully claims a lease:

- The system SHALL look up the Job.
- The stored `JobTraceParent` SHALL be used to initialise the telemetry activity for that operation.
- Telemetry for lease processing SHALL include:
  - `SearchId`
  - `JobId`
  - `LeaseId`
  - `CustodianId`

If the custodian supplies its own `traceparent`, it MAY be recorded for diagnostics but SHALL NOT override the job’s stored trace seed.


### 4.4 Result submission handling

When a custodian submits results:

- `JobId` and `LeaseId` SHALL be required.
- The system SHALL validate that the lease is legitimate.
- The stored `JobTraceParent` SHALL again be used to initialise telemetry.
- Result processing SHALL include `SearchId`, `JobId`, and `LeaseId` as telemetry dimensions.

This ensures that result submission is visually and queryably connected to the originating search.

## 5. Observability Behaviour in Practice

With this design:

- All operations related to a Search share a common trace identifier.
- Application Insights will display a distributed trace graph showing:
  - Search creation
  - Job creation
  - Lease claim
  - Result submission
- Even though these are separate HTTP requests, they appear connected because the trace seed is restored.

Separately, operational queries can always fall back to business identifiers (`SearchId`, `JobId`, `LeaseId`) if trace sampling or header behaviour obscures trace continuity.

---

## 6. What This ADR Explicitly Does Not Do

This ADR does not attempt to:

- Capture custodian-internal spans.
- Require custodians to propagate trace context.
- Depend on external systems for correlation correctness.

Custodian internal tracing is outside FIND’s visibility boundary.

---

## 7. Consequences

### Positive

- Deterministic lifecycle traceability.
- Trace continuity across asynchronous polling boundaries.
- Independence from custodian header propagation behaviour.
- Clear separation between business identity and transport tracing.

### Trade-offs

- Slight storage overhead for persisting trace seed.
- Slight implementation complexity in rehydrating trace context.
- Trace graphs represent FIND’s lifecycle only, not custodian internals.

---

## 8. Summary

Correlation in FIND is rooted in persistent business identifiers and reinforced by stored trace context.

Search is the lifecycle root.

Jobs persist the trace seed.

Lease claims and result submissions rehydrate that seed to maintain distributed trace continuity.

Business identity guarantees correctness.
Trace context improves observability.
They are intentionally separated.
