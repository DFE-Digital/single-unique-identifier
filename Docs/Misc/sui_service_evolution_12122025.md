# SUI National Services – Solution Description

## Introduction

This document describes the SUI National Services architecture at a component level. It explains how responsibility is divided between NHSE and DfE, how identity is resolved and then safely consumed across a distributed ecosystem, and how discovery, access, correction, and lifecycle management are handled in a controlled and extensible way.

A defining characteristic of the solution is that **identity becomes progressively less sensitive as it moves through the system**. This is achieved through a deliberate identifier model and reinforced by clear operational flows for each supported task.

### A note about Identifier Model and Terminology

The architecture relies on three distinct identifiers. Each exists for a specific purpose, within a specific boundary, and with carefully controlled visibility.

#### NHS Number (Authoritative Identifier)

The NHS number is the authoritative identifier used by the current authoritative source. It exists entirely within the NHSE domain and never leaves it in clear form.

It is used exclusively for identity resolution. The NHS number is not exposed to DfE, searching organisations, custodians, or national services outside NHSE.

#### SUI (Single Unique Identifier)

The SUI is the identifier that represents a person within the DfE domain.

It is produced by the NHSE Identifier Issuer Adapter by encrypting the authoritative identifier (currently the NHS number). The SUI is reversible only by NHSE, opaque to all other components, and never leaves the DfE domain.

Within DfE, the SUI is the unifying identifier that ties together discovery, analytics, policy enforcement, and lifecycle management.

#### Subject Identifier (Custodian-Specific Identifier)

The Subject Identifier is the identifier shared with custodians and stored in distributed systems.

It is derived from the SUI, but re-encrypted on a per-custodian or per-organisation basis. Subject Identifiers are non-correlatable outside DfE control and disposable without affecting the underlying SUI.

From a custodian’s perspective, the Subject Identifier is the only identifier they ever see.

---

## Component 1: Identifier Issuer Adapter (NHSE)

The NHSE Identifier Issuer Adapter is the sole integration point with the authoritative source and the only place where the NHS number is ever accessed or used directly.

DfE treats the adapter as a black-box service. It submits demographics and receives a SUI on a confident match. DfE does not know what the authoritative identifier is, nor how it is resolved.

The adapter performs three tightly scoped actions: matching against the authoritative source, retrieving the NHS number on a confident match, and encrypting it to produce a reversible SUI. Authority and reversibility are retained entirely within NHSE.

---

## Component 2: Identity Knowledge & Issuance Registry (DfE)

The Identity Knowledge & Issuance Registry is a core supporting capability within the DfE domain. It replaces the previously described “link table” concept and should not be understood as an operational data store or a source of truth for records or identity.

Instead, the registry records **assertions and issuance events** that arise as a by-product of operating the FIND and FETCH services. It captures what organisations have told the platform about their knowledge of a person, and what identifiers have been issued to them, without storing personal data or authoritative identifiers.

### What the Registry Contains

The registry holds structured information about:

- Organisations and custodians that have interacted with the platform
- The **type of record** a custodian has asserted it holds
- The **SUI** to which that assertion relates (within the DfE domain)
- The **Subject Identifier(s)** that were issued to that organisation or custodian
- The **encryption epoch** under which those Subject Identifiers were derived
- The time at which issuance or knowledge revelation occurred

No NHS numbers are stored. Subject Identifiers and SUIs are recorded only in forms appropriate to lifecycle tracking and governance, not for direct reuse or lookup.

### Purpose and Role

The registry exists to support four distinct but related concerns.

First, it provides a durable record of **identity issuance and knowledge revelation**. When an organisation is issued Subject Identifiers derived from a SUI, or when a custodian confirms that it holds records for a person, that fact is captured explicitly. This enables the platform to understand who has been told what, and when.

Second, it enables **learning and analytics** across the ecosystem. Over time, the registry allows DfE to observe discovery patterns, understand reuse, identify duplication, and reason about how identities and records flow through the system. This insight is essential for service improvement and policy evaluation.

Third, the registry underpins **identifier lifecycle management**. Because Subject Identifiers are derived under a specific encryption epoch, the registry allows the platform to determine which identifiers are affected when an epoch changes. This enables targeted expiry, rotation, or re-issuance of Subject Identifiers without global resets.

Finally, the registry enables **targeted signalling and remediation**. When identifiers need to be rotated, invalidated, or fixed, the platform can identify exactly which custodians and organisations have previously been issued affected identifiers and notify them via the Shared Signals framework. This avoids broadcast invalidation and supports controlled recovery.

### What the Registry Is Not (and How It May Evolve)

The Identity Knowledge & Issuance Registry does not store record content and does not describe the data held by custodians. Custodians remain authoritative for both the existence and content of their records.

The registry is not currently relied upon as a mandatory, synchronous dependency for discovery or retrieval. Its primary purpose is to record knowledge manifests and identifier issuance events, and to support analytics, governance, and identifier lifecycle management, including epoch-based expiry and rotation.

The role of the registry may evolve over time. As knowledge manifests are accumulated and refreshed through interaction with custodians, the platform may choose to use this information more actively to optimise discovery, for example by prioritising known custodians or reducing unnecessary queries. Any such use would build on recorded evidence and would not change the fact that the registry is not authoritative for record content.

---

## Component 3: Find a SUI Service (DfE)

The Find a SUI service is hosted within the **DfE domain**. It is the first DfE-hosted component invoked by a searching organisation and is responsible for issuing **Subject Identifiers** derived from a person’s demographics.

The service does not resolve identity itself and never interacts with the authoritative source directly. Instead, it orchestrates between the searching organisation, the **NHSE Identifier Issuer Adapter**, and the **Identity Knowledge & Issuance Registry**.

### Responsibilities

The Find a SUI service is responsible for:

- accepting demographic records from searching organisations
- delegating identity resolution to NHSE
- deriving organisation-scoped **Subject Identifiers** from a returned **SUI**
- recording **issuance and knowledge information in the registry**
- handling non-match scenarios via an explicit exception path

### Chronology

When a searching organisation requests a Subject Identifier, the following sequence occurs:

1. A demographic record is submitted to the Find a SUI service.
2. The service calls the **NHSE Identifier Issuer Adapter** to resolve the person.
3. The adapter performs matching against the authoritative source.
4. On a confident match, the adapter encrypts the authoritative identifier and returns a **SUI** to DfE.
5. The Find a SUI service derives a **Subject Identifier scoped to the searching organisation** from the SUI.
6. Issuance details are recorded in the **Identity Knowledge & Issuance Registry**, including:
   - the SUI involved
   - the Subject Identifier issued
   - the searching organisation it was issued to
   - the date of issuance
   - the encryption epoch used
7. The Subject Identifier(s) are returned to the searching organisation.

If a confident match cannot be achieved, no identifiers are issued. The request follows a separate exception path, integrating with business process or national back-office capability to allow manual or assisted resolution outside the automated flow.

Through this approach, the Find a SUI service enforces a clean separation between **identity resolution** (NHSE) and **identity usage** (DfE), while establishing the foundation for lifecycle management, analytics, and targeted remediation.

---

## Component 4: Find a Record Service (DfE)

The Find a Record service is hosted within the **DfE domain**. It is responsible for determining **which custodians hold records** for a given person, without retrieving record content.

The service operates on **Subject Identifiers** and does not perform identity resolution. It collects and maintains internal knowledge about record existence, and then applies data sharing policy to decide whether knowledge of that existence can be shared between parties.

### Responsibilities

The Find a Record service is responsible for:

- accepting Subject Identifiers from searching organisations
- identifying custodians that have previously asserted knowledge of the person
- actively querying custodians where required
- recording new knowledge assertions in the registry
- applying data sharing policy to determine what knowledge may be disclosed
- returning record location information without exposing record content

### Chronology

When a searching organisation requests to find records for a person, the following sequence occurs:

1. A **Subject Identifier scoped to the searching organisation** is submitted to the Find a Record service.
2. The **Identity Knowledge & Issuance Registry** is consulted to identify custodians that have previously asserted knowledge of the person associated with the underlying SUI.
3. Where necessary, the service derives **custodian-scoped Subject Identifiers** and queries custodians directly to determine whether they hold relevant records.
4. Any positive custodian response is treated as a new **knowledge assertion**.
5. The registry is updated to record:
   - the custodian asserting knowledge
   - the record type asserted
   - the SUI and custodian-scoped Subject Identifier involved
   - the date of the assertion
6. Once knowledge has been collected, the service evaluates **data sharing policy** to determine whether knowledge of record existence may be disclosed to the searching organisation, based on:
   - the parties involved
   - the stated purpose
   - any applicable time constraints
7. The service returns record location information to the searching organisation, typically in the form of pointers or manifests, subject to the outcome of the policy decision.

The Find a Record service never retrieves record content and never exposes SUIs or authoritative identifiers. Custodians remain authoritative for the existence and content of their data.

Through this approach, the service cleanly separates **internal knowledge collection** from **external disclosure**, allowing the platform to learn over time while ensuring that sharing of knowledge is always explicitly authorised.

---

## Component 5: Fetch a Record Service (DfE)

The Fetch a Record service is hosted within the **DfE domain**. It acts as a **secure proxy** between consumers of record data (searching organisations) and producers of that data (custodians).

The service controls and mediates access to record content. It hides custodian endpoints behind DfE-managed URLs, enforces data sharing agreements, and ensures that access is authorised, time-limited, and schema-compliant.

### Responsibilities

The Fetch a Record service is responsible for:

- accepting requests to retrieve specific records identified during discovery
- enforcing data sharing agreements (DSAs) before any access is granted
- acting as a proxy between searchers and custodians
- shielding custodian URLs from direct exposure
- issuing secure, time-limited access to record content
- enforcing **record-type-specific data schemas**, including version control
- coordinating retrieval without storing record content within DfE

### Chronology

When a searching organisation requests to fetch a record, the following sequence occurs:

1. A fetch request is submitted to the Fetch a Record service, referencing a record previously identified by the Find a Record service.
2. The service evaluates the relevant **data sharing agreement (DSA)** to determine whether access to the requested record is permitted for the stated purpose and time window.
3. If access is permitted, the service prepares a **DfE-managed access URL** representing the record to be retrieved.
4. The Fetch a Record service derives a **custodian-scoped Subject Identifier** where required and uses it to request the record from the custodian.
5. The custodian returns the record content to the Fetch a Record service.
6. The service validates the returned content against the **expected schema for the record type and version**.
7. If validation succeeds, the record is made available to the searching organisation via the secure access URL.
8. Access to the URL is restricted to the requesting organisation and is valid only for a limited period of time.

If the DSA does not permit access, or if the returned content does not conform to the expected schema, the request is rejected and the record is not disclosed.

The Fetch a Record service never exposes custodian endpoints, never exposes SUIs or authoritative identifiers, and does not persist record content. It serves as a **policy, security, and contract enforcement boundary** between data producers and consumers.

---

## Component 6: Shared Signals Service (DfE)

The Shared Signals service is hosted within the **DfE domain**. It provides an **eventing and notification mechanism** that allows changes affecting identifiers or knowledge manifests to be communicated to interested parties in a controlled and targeted way.

Signals may originate **within DfE** (for example, as a result of identifier lifecycle events such as key rotation) or **within NHSE** (for example, where authoritative correction or record-fixing activity is required). The service acts as a single, consistent distribution point regardless of where a signal originates.

The service does not resolve identity, discover records, or retrieve data. Its purpose is to notify relevant parties when something they have previously interacted with has changed.

### Responsibilities

The Shared Signals service is responsible for:

- receiving signals originating from **DfE services** and from **NHSE** (via an adapter service)
- validating the source and type of each signal
- determining which organisations or custodians are affected
- enforcing subscription and authorisation rules
- delivering notifications only to relevant parties
- triggering internal lifecycle or remediation actions where required

### Chronology

When a signal is raised, the following sequence occurs:

1. A signal is raised by an authorised source, either:
   - from within **DfE** (e.g. identifier rotation, epoch change), or
   - from within **NHSE** (e.g. authoritative correction or record-fix activity).
2. The Shared Signals service receives and validates the signal.
3. The **Identity Knowledge & Issuance Registry** is consulted to determine:
   - which organisations have been issued affected Subject Identifiers
   - which custodians have previously asserted knowledge related to the affected SUI
4. Subscription and authorisation rules are applied to determine which parties should be notified.
5. Notifications are delivered to affected organisations and custodians.
6. Where appropriate, internal DfE services are notified to trigger follow-on actions, such as re-issuance of Subject Identifiers or initiation of fix workflows.

Signals do not contain NHS numbers, SUIs, Subject Identifiers, or record content. They communicate only that a change has occurred and that action may be required.

Through this approach, the Shared Signals service enables **targeted, proportionate notification** across the ecosystem, supporting identifier lifecycle management and remediation without relying on polling or broadcast messaging.

---

## Component 7: Exceptions Path & Back-Office Integration (DfE)

The Exceptions Path and Back-Office Integration provides a controlled mechanism for handling scenarios that cannot be resolved through the automated FIND and FETCH flows.

The primary resolver for such exceptions is expected to be the **National Back Office team**. The integration is deliberately abstracted from any specific delivery mechanism, allowing cases to be routed through different operational systems over time without impacting core services.

### Responsibilities

The Exceptions Path integration is responsible for:

- receiving exception events from DfE services
- creating and tracking exception cases for operational resolution
- routing cases to the National Back Office team
- abstracting the underlying case management or delivery platform
- supporting future integration with operational tooling (e.g. ServiceNow)

### Chronology

When an exception occurs, the following sequence takes place:

1. An exception is raised by a DfE service, for example:
   - identity resolution cannot be completed
   - a record cannot be retrieved despite prior discovery
   - schema validation or data correction is required
2. The exception is passed to the Exceptions Path integration.
3. A case is created and routed to the **National Back Office team** for investigation and resolution.
4. The method of delivery (e.g. API integration, workflow system, ticketing platform) is handled behind an abstraction layer.
5. The National Back Office team investigates and resolves the issue using appropriate business processes.
6. Where relevant, outcomes may result in follow-on actions such as:
   - identifier re-issuance
   - record correction
   - triggering Shared Signals to affected parties

The Exceptions Path does not perform identity resolution, data correction, or record modification itself. Its role is to provide a reliable bridge between automated services and human-led operational processes.

By abstracting the delivery mechanism, the architecture remains flexible. While initial implementations may integrate with systems such as **ServiceNow**, the core FIND, FETCH, and SIGNALS services remain insulated from changes to back-office tooling.

---
## Footnote: On Encrypted Identifiers and the Emergence of a Registry

As the design has matured, it has become increasingly clear that the use of encrypted identifiers alone is not sufficient to support the full set of lifecycle and governance requirements.

Issuance date, expiry, and rotation cannot be embedded into an encrypted identifier without breaking its deterministic properties. Preserving determinism therefore requires issuance metadata to be stored elsewhere. Similarly, targeted remediation — such as invalidation, rotation, or fix activity — requires knowledge of which organisations and custodians have previously been issued identifiers, unless changes are broadcast indiscriminately.

In practice, any custodian in possession of an encrypted identifier must, by definition, have interacted with the national matching service at some point. That interaction itself constitutes knowledge that must be recorded if the system is to support lifecycle management, analytics, and targeted signalling.

Taken together, these constraints point consistently toward the need for a registry of issuance and knowledge. The resulting question is not whether such a registry exists, but what role encrypted identifiers continue to play alongside it. In this architecture, encryption remains valuable for scoping, non-correlation, and minimising exposure, while the registry provides the necessary context to manage identifiers safely and deliberately over time.
