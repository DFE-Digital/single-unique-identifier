# Overview

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
  MATCH, FIND, or related national services).

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
   A pivotal ADR formally adopts use of the NHS number in the clear as the Single Unique
   Identifier, reframing several earlier decisions.

5. **Current focus**  
   Current ADRs concentrate on discovery interaction models, lifecycle management in the
   post-encryption world, and unresolved areas such as authentication and trust.

This index and the ADR set should be read with that progression in mind.

---

## Summary of ADRs

| ADR | Title | Category | Status | Notes |
|-----|------|----------|--------|-------|
| 0001 | Build central locator and LA generator | Systems landscape | Draft | Early programme scaffolding |
| 0002 | Create new component for central matching service | Systems landscape | Draft | MATCH service foundations |
| 0003 | Testing reg-n-roll (Playwright) | Component | Draft | Development and testing approach |
| 0004 | Mocking tool | Component | Draft | Local and integration testing support |
| 0005 | Compiled DSA policy and policy enforcement at FIND and FETCH boundaries | Systems landscape | Draft | Conceptually sound; work paused pending national DSA clarity |
| 0006 | Custodian-scoped identifiers to hide the underlying NHS number | Systems landscape | Superseded | Relevant only under encrypted-identifier assumptions |
| 0007 | Deterministic cryptographic scheme and key management for SUI identifiers | Systems landscape | Superseded | Superseded alongside custodian-scoped identifiers |
| 0008 | Lifecycle, expiry and invalidation of issued SUI identifiers | Systems landscape | Superseded | Superseded by ADR-0013 following identity pivot |
| 0009 | Central custodian knowledge register for identity and discovery | Systems landscape | Accepted | Supports both historic and current discovery models |
| 0010 | Use of NHS number as the single unique identifier | Systems landscape | Accepted | Pivotal architectural decision |
| 0011 | Authentication and trust boundaries for SUI APIs | Systems landscape | Draft | Incomplete; to be developed further |
| 0012 | Asynchronous pull-based discovery for FIND | Systems landscape | Proposed | Current active discovery design |
| 0013 | Lifecycle management of shared SUI identifiers | Systems landscape | Draft | Replacement for ADR-0008 in post-encryption architecture |
| 0014 | Demographic Event Integration for Identifier Lifecycle Triggers (NEMS / MNS) | Systems landscape | Not Started | Integrating the National discovery service with NHS demographic event notification services |
| 0015 | Optional Webhook Notifications for Task Availability | System landscape | Not Started | Webhook-based notifications as an optional enhancement to the pull-based polling model |
| 0016 | API Edge Pattern for National Services | System landscape | Not Started | How the national discovery APIs should be exposed and protected at the platform edge |
| 0017 | Exposure Model for National APIs (Public vs Private)| System landscape | Not Started | Public vs Private network posture |
| 0018 | High-Performance Polling for Pull-Based Discovery | System landscape | Proposed | Achieving high performance, low latency polling |
| 0019 | Job Broker and Lease State | Proposed | Light weight job broker and lease state management |

---

## Maintaining this index

This index is expected to evolve.

When new ADRs are added:
- update the summary table,
- add short notes if the ADR represents a pivot or supersedes earlier work,
- and, where helpful, extend the architectural evolution narrative above.

The goal is that this file remains the **starting point** for understanding the SUI
architecture and its decision history.
