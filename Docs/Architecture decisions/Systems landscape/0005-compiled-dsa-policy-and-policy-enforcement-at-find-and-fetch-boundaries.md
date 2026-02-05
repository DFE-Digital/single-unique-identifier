# ADR-SUI-0005: Compiled DSA Policy and Policy Enforcement at FIND and FETCH Boundaries

Date: 2025-11-11

Author: Simon Parsons

Category: Systems landscape

## Status

Draft

## Context

FIND-A-RECORD and FETCH-A-RECORD operate across multiple organisations participating in a national data sharing service.

- **FIND-A-RECORD** reveals which organisations hold records about a subject. It returns pointers or manifests (knowledge that records exist), not the records themselves.
- **FETCH-A-RECORD** obtains and returns record content where policy allows.

Data sharing between organisations is governed by Data Sharing Agreements (DSAs). At the time of writing:

- The exact DSA schema and list of attributes is not final.
- DSAs are expected to express rules based on stable properties of:
  - the source organisation,
  - the destination organisation,
  - the type and sensitivity of the data,
  - the mode of sharing (existence-only vs content),
  - the purpose or context for access,
  - and possibly other factors (for example geography, statutory role, or specific bilateral arrangements).

DSAs must be enforced:

- just before FIND-A-RECORD reveals that records exist for a given subject at a given organisation; and
- just before FETCH-A-RECORD returns record content.

We require:

- strong guarantees around correctness and auditability;
- support for time-bounded rules (e.g. “between these dates”);
- support for explicit bilateral or targeted rules (“ORG-A may share X with ORG-B”);
- a way to apply urgent revocations safely (“stop sharing this now”);
- very fast evaluation suitable for high volumes of candidate records and organisations.

We cannot rely on manually interpreting legal text at runtime, nor repeatedly parsing and walking raw JSON rules for each request.

## Problem

How do we:

1. Represent DSAs so they are:
   - governable and versioned,
   - expressive enough for real-world constraints,
   - but not coupled to our runtime implementation details?

2. Apply DSA rules:
   - at the **boundary of FIND-A-RECORD**, for every candidate “pointer”,
   - at the **boundary of FETCH-A-RECORD**, for every candidate record content response,
   - in a way that is:
     - deterministic,
     - auditable,
     - fast (no ad hoc JSON parsing or rule engines on the hot path),
     - safe in the face of policy changes.

3. Ensure that the policy enforced by the PEP is aligned with the latest approved DSAs (HEAD), so:
   - urgent revocations cannot be silently ignored;
   - we can detect if a PEP is using a stale compiled view.

4. Do this while the exact DSA schema is still evolving.

## Decision

### 1. DSAs as JSON source of truth

Each participating organisation’s DSA is represented as a JSON document stored in a central DSA Registry (for example, Cosmos DB).

Characteristics:

- One primary DSA document per organisation (as **source** of sharing).
- Documents may contain:
  - default rules,
  - exception rules,
  - explicit `destOrgIds` for bilateral/multilateral overrides,
  - more general criteria (such as destination organisation type),
  - optional `validFrom` and `validUntil` for time-bounded rules.
- DSAs are governed, versioned, and approved through operational and legal processes, not application code.
- FIND/FETCH do **not** interpret these JSON rules directly at runtime.

Example (illustrative, not final schema):

```json
{
  "orgId": "ORG-A",
  "version": "2025-11-10T12:00:00Z",
  "defaults": [
    {
      "effect": "allow",
      "modes": ["EXISTENCE"],
      "dataTypes": ["SAFEGUARDING_PTR"],
      "destOrgTypes": ["LOCAL_AUTHORITY", "HEALTH", "POLICE"],
      "purposes": ["SAFEGUARDING"],
      "validFrom": "2025-01-01T00:00:00Z"
    }
  ],
  "exceptions": [
    {
      "effect": "allow",
      "modes": ["CONTENT"],
      "dataTypes": ["FULL_CASE_FILE"],
      "destOrgIds": ["ORG-B"],
      "purposes": ["SAFEGUARDING"],
      "validFrom": "2025-06-01T00:00:00Z",
      "validUntil": "2026-06-01T00:00:00Z"
    },
    {
      "effect": "deny",
      "modes": ["CONTENT"],
      "dataTypes": ["FULL_CASE_FILE"],
      "destOrgTypes": ["VOLUNTARY"],
      "purposes": ["GENERAL_ENQUIRY"]
    }
  ]
}
```

### 2. Compiled policy artefact

We introduce a **Policy Compiler** service (for example, an Azure Function) that:

- reads all current DSA documents and organisation metadata;
- maps relevant values to dense identifiers:
  - `OrgCode → int`,
  - `DataType → int`,
  - `Purpose → int`,
  - `Mode → int`,
  - (and any other stable, low-cardinality attributes);
- evaluates all rules that are active at compile time (respecting `validFrom` / `validUntil`);
- for each relevant combination `(sourceOrg, mode, dataType, purpose, ...)`:
  - calculates the set of destination organisations that are allowed,
  - encodes that set as a `bool[]` or bitset for constant-time membership tests;
- ensures specific exceptions override general defaults deterministically;
- packages this into an immutable **Compiled Policy Artefact** with:
  - the mapping tables,
  - the matrix/bitsets of effective allows,
  - a `policyVersionId` computed from the underlying DSAs and their versions.

The compiled policy artefact is persisted in a durable configuration store (for example Azure Blob Storage) and loaded into in-memory structures by the PEP. A distributed cache (for example Azure Cache for Redis) may be used to accelerate distribution to multiple instances, but all runtime decisions are made against in-memory representations, not over the network.

This compilation step is where we pay the complexity cost once, so that runtime decisions in the PEP are a single lookup plus a bit test.

### 3. PolicyHead for strong alignment with DSAs

To ensure PEPs never silently use an unknown or partial view of policy, we introduce a `PolicyHead` record in the DSA Registry:

```json
{
  "id": "policy-head",
  "currentPolicyVersionId": "v-A1B2C3D4",
  "compiledAtUtc": "2025-11-10T12:05:00Z"
}
```

Rules:

- Only the Policy Compiler updates `PolicyHead`.
- A new or changed DSA is not considered in force for runtime enforcement until:
  - a compile has successfully produced a new artefact, and
  - `PolicyHead.currentPolicyVersionId` has been moved to reference it.
- This gives a clear, auditable link:
  - **DSA set** → **compiled artefact** → **policy version in use**.

### 4. Policy Enforcement Point (PEP)

We introduce a dedicated PEP component (for example, an Azure Function plus in-process client) used by both FIND-A-RECORD and FETCH-A-RECORD.

The PEP:

- On startup and periodically:
  - reads `PolicyHead`,
  - loads the corresponding compiled artefact from blob storage,
  - caches it in memory.
- For each decision, evaluates a `ShareContext`:

  ```json
  {
    "sourceOrgCode": "ORG-A",
    "destOrgCode": "ORG-B",
    "dataType": "SAFEGUARDING_PTR",
    "purpose": "SAFEGUARDING",
    "mode": "Existence"
  }
  ```

- Performs:
  - mapping of codes to internal IDs,
  - an index calculation,
  - a single lookup into the relevant bitset row to see if `destOrg` is allowed.
- Returns:
  - `isAllowed` (true/false),
  - `reason` (short explanation),
  - `policyVersionId` used for the decision.

If the cached `policyVersionId` does not match `PolicyHead.currentPolicyVersionId`, the PEP:

- reloads the compiled artefact for the new version; and
- if a valid artefact cannot be loaded, fails closed for sensitive operations and logs an error, rather than silently using a stale policy.

### 5. Applying the DSA at the boundary of FIND-A-RECORD

For FIND-A-RECORD, enforcement happens explicitly at the output boundary, **per pointer**.

Flow (simplified):

1. The Searcher (requesting organisation) calls FIND-A-RECORD.
2. FIND-A-RECORD queries custodians or an index and obtains a set of candidate pointers:
   - each pointer includes at least:
     - `sourceOrgId` (custodian),
     - a pointer `dataType` (for example `SAFEGUARDING_PTR`),
     - the `purpose` (per API contract),
     - the Searcher’s `destOrgId`.
3. Before including each pointer in the response, FIND-A-RECORD calls the PEP with:
   - `mode = EXISTENCE`,
   - that pointer’s `sourceOrgId`, the Searcher’s `destOrgId`,
   - the pointer’s data type,
   - the declared purpose.
4. Only if the PEP returns `isAllowed = true` is the pointer included in the outbound response.
5. Each PEP decision is logged with `policyVersionId` for audit.

This guarantees: **no organisation learns that another holds a record unless the current compiled DSA policy explicitly permits that existence-level disclosure.**

### 6. Applying the DSA at the boundary of FETCH-A-RECORD

FETCH-A-RECORD uses the same mechanism for content:

1. The consumer (often the same Searcher) requests record content.
2. FETCH-A-RECORD identifies:
   - `sourceOrgId` (custodian),
   - `destOrgId` (requesting organisation),
   - `dataType` for the requested content,
   - the `purpose`,
   - `mode = CONTENT`.
3. FETCH-A-RECORD calls the PEP with this context before returning any content.
4. If allowed, content is returned. If denied, an appropriate response is given and logged.

This cleanly separates:

- existence-only policy (FIND),
- content-access policy (FETCH),

while using the same compiled artefact and enforcement mechanism.

## Policy Freshness, Time-Bounded Rules, and Revocation

- Time-bounded rules (`validFrom`, `validUntil`) are applied at **compile time**:
  - only rules active at `nowUtc` are included in the compiled artefact;
  - expired or not-yet-active rules are ignored for that version.
- The Policy Compiler can be triggered by:
  - DSA changes (for example via a change feed or event),
  - and/or a scheduled run to pick up rules entering or leaving their validity window.
- Because the PEP always aligns to `PolicyHead.currentPolicyVersionId`:
  - we can guarantee that decisions are based on a known, complete compiled view.
- For urgent “stop sharing now” scenarios:
  - the DSA management flow can be implemented so that updating the DSA and recompiling is treated as a single operation, only updating `PolicyHead` when a new artefact is available;
  - PEPs then move to the new policy as soon as they observe the updated `PolicyHead`, and never knowingly use an artefact that does not match HEAD.

If the PEP cannot obtain the artefact referenced by `PolicyHead`, it must:

- log the failure; and
- fail closed for any new protected disclosures until a valid policy is available.

## Scope and Limitations

This compiled-policy and PEP model is intentionally scoped to:

- organisation-level and other low-cardinality, attribute-based rules;
- explicit bilateral and multilateral overrides (`destOrgIds`);
- separation of existence vs content access via `mode`;
- time-bounded rules via validity windows and recompilation.

It is **not** intended to encode:

- rate limiting or behavioural detection;
- workflow- or history-dependent conditions;
- complex temporal logic beyond coarse validity windows;
- rules that require inspecting raw record content directly.

Those concerns are to be addressed by:

- API gateways and rate limiting,
- case management and workflow engines,
- content classification and custodian-side controls,

working alongside this DSA enforcement layer.

## Consequences

- Introduces:
  - a DSA Registry (JSON as source of truth),
  - a Policy Compiler (builds compiled artefacts),
  - a `PolicyHead` pointer,
  - a shared PEP used by FIND-A-RECORD and FETCH-A-RECORD.
- Provides:
  - fast, deterministic decisions suitable for high volume,
  - clear enforcement at the FIND/FETCH boundaries,
  - explicit support for bilateral rules and time-bounded agreements,
  - robust alignment between DSAs and the policies actually enforced.
- Requires:
  - disciplined operational handling of DSA updates and compilation,
  - complementary controls for behaviours and conditions outside this model’s scope.
