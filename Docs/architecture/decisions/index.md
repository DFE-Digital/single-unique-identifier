# SUI Architecture Decision Records

Architectural decisions in the Single Unique Identifier (SUI) programme are captured using
Architecture Decision Records (ADRs).

The ADRs exist to make architectural reasoning explicit: what was decided, why it was
decided at the time, and how that decision influenced subsequent work. They are not
intended to describe a single, static end-state architecture. Instead, they document the
evolution of the architecture as constraints, assumptions, and national dependencies
became clearer.

Since decisions apply at different scopes, ADRs are structured into categories:

- **Systems landscape**  
  Decisions that affect the overall shape of the SUI programme, including identity models,
  discovery patterns, trust boundaries, and governance.

- **System**  
  Decisions about the structure and responsibilities of individual systems (for example,
  Get an Identifier or the Notification Service).

- **Component**  
  Decisions about internal component design within a system.

---

## How to read these ADRs

The ADRs should be read as a **timeline**, not as a flat list.

Several early ADRs explore design options that were later superseded as the programme
moved through Discovery and assumptions were tested against operational reality. Where this
has happened, those ADRs are intentionally retained and explicitly marked as superseded,
rather than rewritten or deleted.

A new architect joining the programme should expect to see:

- early decisions that explore privacy-preserving and cryptographic approaches,
- explicit pivots where those approaches were abandoned in favour of simpler or more
  operationally viable models,
- and newer ADRs that anchor the current direction of travel.

Where relevant, ADRs reference each other to make these relationships explicit.

---

## Architectural evolution (high-level timeline)

At a high level, the SUI architecture has evolved through the following phases:

1. **Foundational exploration**  
   Early ADRs focus on how to support cross-organisational discovery while minimising the
   spread of sensitive identifiers and enforcing data sharing rules.

2. **Privacy-first identity design**  
   Several ADRs explore custodian-scoped identifiers and deterministic cryptographic schemes
   as a way to hide the NHS number while still enabling correlation.

3. **Operational and ecosystem reassessment**  
   As the programme progressed, the cost, complexity, and operational friction of encrypted
   identifiers became clearer.

4. **Architectural pivot**  
   The implementation uses the NHS number as the identifier, reframing earlier encrypted-identifier designs. ADR-SUI-0010 records that direction but still has Proposed status.

5. **Current Alpha direction**
   Get an Identifier searches PDS using vendor-supplied demographics and returns the matched NHS number and GP practice ODS code where available. Accepted Get an Identifier ADRs select MESH for receiving MNS lifecycle notifications and asynchronous duplicate-subscription cleanup. A separate Notification Service is intended to notify registered supplier webhooks so suppliers rematch through Get an Identifier.

PDS matching is implemented. MNS subscription creation and end-to-end notification processing are not implemented on `main`. The Notification Service retrieves and parses MESH messages. Register and signed delivery components exist, but the orchestrator does not dispatch lifecycle notifications or acknowledge MESH messages. See the [as-built design](../../Design/GetAnIdentifier/AsBuilt.md), [Notification Service README](../../../Apps/NotificationService/README.md) and [draft supplier lifecycle contract](../../Design/Notifications-Webhooks/SupplierLifecycle/V1/Index.md).

Earlier distributed discovery, polling, jobs and `MATCH` / `FIND` / `FETCH` decisions retain their recorded status and historical context. Acceptance within an earlier design does not make that design the current Alpha scope.

This index and the ADR set should be read with that progression in mind.

---

## Summary of ADRs

| ADR  | Title                                                                        | Category          | Status                | Notes                                                                                       |
| ---- | ---------------------------------------------------------------------------- | ----------------- | --------------------- | ------------------------------------------------------------------------------------------- |
| 0001 | Build central locator and LA generator                                       | Systems landscape | Accepted              | Early programme scaffolding                                                                 |
| 0002 | Create new component for central matching service                            | Systems landscape | Accepted              | MATCH service foundations                                                                   |
| 0003 | Testing reg-n-roll (Playwright)                                              | Component         | Accepted              | Development and testing approach                                                            |
| 0004 | Mocking tool                                                                 | Component         | Draft                 | Local and integration testing support                                                       |
| 0005 | Compiled DSA policy and policy enforcement at FIND and FETCH boundaries      | Systems landscape | Draft                 | Conceptually sound; work paused pending national DSA clarity                                |
| 0006 | Custodian-scoped identifiers to hide the underlying NHS number               | Systems landscape | Rejected / superseded | Relevant only under encrypted-identifier assumptions                                        |
| 0007 | Deterministic cryptographic scheme and key management for SUI identifiers    | Systems landscape | Rejected / superseded | Superseded alongside custodian-scoped identifiers                                           |
| 0008 | Lifecycle, expiry and invalidation of issued SUI identifiers                 | Systems landscape | Superseded            | Superseded by ADR-0013 following identity pivot                                             |
| 0009 | Central custodian knowledge register for identity and discovery              | Systems landscape | Accepted              | Accepted within the earlier discovery design; not evidence of an implemented Alpha register |
| 0010 | Use of NHS number as the single unique identifier                            | Systems landscape | Proposed              | NHS number direction reflected in current matching implementation                           |
| 0011 | Authentication and trust boundaries for SUI APIs                             | Systems landscape | Draft                 | Incomplete; to be developed further                                                         |
| 0012 | Asynchronous pull-based discovery for FIND                                   | Systems landscape | Proposed              | Earlier proposed distributed discovery design                                               |
| 0013 | Lifecycle management of shared SUI identifiers                               | Systems landscape | Draft                 | Replacement for ADR-0008 in post-encryption architecture                                    |
| 0014 | Demographic Event Integration for Identifier Lifecycle Triggers (NEMS / MNS) | Systems landscape | Draft                 | Earlier lifecycle framing; see accepted Get an Identifier MNS ADRs                          |
| 0015 | Optional Webhook Notifications for Task Availability                         | System landscape  | Draft                 | Webhook-based notifications as an optional enhancement to the pull-based polling model      |
| 0016 | API Edge Pattern for National Services                                       | System landscape  | Draft                 | How the national discovery APIs should be exposed and protected at the platform edge        |
| 0017 | Exposure Model for National APIs (Public vs Private)                         | System landscape  | Draft                 | Public vs Private network posture                                                           |
| 0018 | High-Performance Polling for Pull-Based Discovery                            | System landscape  | Proposed              | Achieving high performance, low latency polling                                             |
| 0019 | Job Broker and Lease State                                                   | System Landscape  | Proposed              | Light weight job broker and lease state management                                          |
| 0020 | Correlation and Trace Continuity Across Distributed Search and Polling       | System Landscape  | Proposed              | Correlation of requests                                                                     |

---

## Accepted Get an Identifier decisions

| ADR                                                                              | Status   | Decision                                                         |
| -------------------------------------------------------------------------------- | -------- | ---------------------------------------------------------------- |
| [GetAnIdentifier-0001](./System/GetAnIdentifier/0001-NHS-MNS-integration.md)     | Accepted | Receive MNS notifications through MESH for Alpha.                |
| [GetAnIdentifier-0002](./System/GetAnIdentifier/0002-mns-duplicate-avoidance.md) | Accepted | Remove duplicate subscriptions asynchronously after subscribing. |

These are decisions, not claims that the full flow is implemented. The [ADR file inventory](./overview.md) includes links to the individual landscape and system records; each record's own status is authoritative.

For the intended environment-specific gateway and authentication choices and their rationale, see the [Get an Identifier authentication matrix](../../Design/GetAnIdentifier/AsBuilt.md#authentication-by-environment). That section distinguishes the target strategy from repository configuration and live settings that still need verification.

## Maintaining this index

This index is expected to evolve.

When new ADRs are added:

- update the summary table,
- add short notes if the ADR represents a pivot or supersedes earlier work,
- and, where helpful, extend the architectural evolution narrative above.

The goal is that this file remains the **starting point** for understanding the SUI
architecture and its decision history.
