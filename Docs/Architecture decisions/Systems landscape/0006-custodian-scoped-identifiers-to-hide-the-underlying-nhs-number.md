# ADR-SUI-0006: Custodian-Scoped Identifiers to Hide the Underlying NHS Number

Date: 2025-12-08

Author: Simon Parsons

Category: Systems landscape

## Status

Rejected - 2025-12-08 - Superceded by decision to use NHS number in the clear

## Context

To support the national **Record Locator / FIND** service, custodians must store a **new** identifier that enables matching of person records across organisations. The goal is to support reliable correlation without requiring custodians to handle the raw NHS number, which is treated as sensitive and whose proliferation into new domains would increase risk.

The NHS number provides the most stable and complete foundation for correlation today, due to its nationwide coverage and authoritative maintenance via PDS. However, raw NHS number storage in systems that have not previously needed it raises justified concerns around privacy boundaries and expanding the impact of data breaches.

Three options were assessed:

| Option | Description | Strengths | Limitations |
|--------|-------------|-----------|-------------|
| 1 | Store raw NHS number in custodian systems | Simple, well understood | Expands sensitive identifier into new systems and increases blast radius if leaked |
| 2 | Share a **single encrypted identifier** across all custodians | Initially seems low-risk; avoids storing raw NHS number | As adoption grows, the encrypted identifier becomes ubiquitous and thus effectively **just as sensitive** as the NHS number |
| 3 | Use a **custodian-scoped encrypted identifier** derived from NHS number | Identifiers are harmless outside the custodian; correlation only possible via FIND | Requires orchestration from the national service |

Option **3** is the only approach that:

- prevents uncontrolled cross-system correlation  
- contains the consequences of a breach to a single custodian  
- maintains clear trust boundaries while enabling national-level linking  

This approach also extends naturally if a future national identifier replaces NHS number.


## Decision

Custodians will store a **custodian-scoped encrypted identifier** for each person record.

Demographic attributes (e.g. name, date of birth, address) may be used **only to resolve the authoritative SUI** *(currently the NHS number via PDS)*.  
They **do not form part** of the encrypted identifier itself.  
The identifier is generated solely from the SUI and the custodian’s encryption context.

This custodian-scoped encrypted identifier:

- Is generated centrally using the SUI  
- Produces the **same value** for the same SUI **within** a custodian’s boundary  
- Produces a **different value** for the same SUI **across** custodians  

Cross-custodian correlation is only possible via the national service.

## High-Level Operational Flows

### Identifier Issuance (Custodian Onboarding)
1. Custodian submits demographics to **MATCH**  
2. MATCH validates identity via PDS → SUI obtained *(currently NHS number)*  
3. National service encrypts the SUI → custodian-scoped encrypted identifier  
4. Custodian stores identifier against its person record  

### Discovery & Correlation (FIND)
Every participating organisation may act as a **Searcher** and/or **Custodian**.  
Each organisation has its own **unique** scoped identifier for the same person.

Flow:

1. The Searcher provides **its own** scoped identifier  
2. FIND resolves that identifier → SUI *(centrally — Searcher cannot do this)*  
3. For each custodian:
   - FIND **re-encrypts** the SUI into that custodian’s scope  
   - FIND requests that custodian’s **record manifest** for that identifier  
4. The custodian responds with a **manifest** containing **only**:
   - **Record types** they hold for that person  
   - **Pointers/URLs** to those records  
   *(but **not** the record content itself)*  
5. Retrieval of **record content** is handled separately  
   *(covered in a separate ADR — FETCH)*

Raw SUI values never leave trusted central services.  
FIND is the **only** component capable of cross-custodian correlation.

## Consequences

### Custodian responsibilities
- Store and index the custodian-scoped encrypted identifier  
- Provide a secure interface to check record presence using that identifier  
- Support periodic **refresh** of stored identifiers when instructed by the national service  
- Ensure newly created or historical records are **populated** with identifiers as onboarding and enrichment continues  

## Related Decisions
- **ADR-SUI-0007** — Cryptographic scheme and key management  
