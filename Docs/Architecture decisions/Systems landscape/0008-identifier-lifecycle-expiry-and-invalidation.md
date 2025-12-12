# ADR-SUI-0008: Identifier Lifecycle, Expiry and Invalidation

**Date:** 2025‑12‑11  
**Status:** Draft  
**Category:** Architecture Decision Record


## 1. Context

A service generates **deterministic identifiers** for use by multiple organisations.  
The identifier is produced as a pure function of:

```
ID = F(Organisation, SUI, Epoch)
```

Where:

- **Organisation** = the party holding the identifier  
- **SUI** = the subject identifier (not stored centrally by the service)  
- **Epoch** = cryptographic key epoch  
- **F** = deterministic encryption + encoding scheme  

Determinism is essential because:

- When an identifier is presented, the service must decrypt it to recover the underlying SUI.
- The service must then generate the **exact** identifier that another organisation would have stored for the same subject.
- Organisations must never store different identifiers for the same subject within the same epoch.

At the same time, the system requires:

- **Key rotation**  
- **Expiry** of identifiers after a certain lifetime  
- **Invalidation** of identifiers issued before a subject‑level correction event  
- **No central storage of SUI**  
- **No randomness** in the identifier  

These requirements bring the lifecycle problem into conflict with determinism.


## 2. Problem

Two lifecycle behaviours require knowledge of **when an identifier was issued**:

### 2.1 Time‑based expiry  
Policies such as:

- *“Identifiers older than N months must be refreshed.”*

These require the system to answer:

> **When was this ID first issued?**

### 2.2 Invalidation of identifiers issued before a correction  
If a subject’s SUI was corrected, merged, or superseded, policy requires:

- *“Invalidate any identifier issued for this SUI before time T.”*

Again, the service must know:

> **Was this ID issued before or after correction time T?**

### But the identifier must remain deterministic

Identifiers **cannot include:**

- Issuance timestamp  
- TTL  
- Randomness  
- Correction markers  

Anything tied to time or one‑off state would cause:

- The same `{Organisation, SUI, Epoch}` to yield **different IDs** at different times.  
- The service to be unable to reconstruct the identifier that an organisation already stored.

This breaks cross‑organisation lookup completely.

Thus:

> The identifier cannot contain issuance information.  
> The service still needs issuance information.  
> This creates an unavoidable tension.


## 3. Alternatives Considered and Rejected

### 3.1 Encode IssuedAt or TTL inside the identifier  
**Rejected** because:

- It destroys determinism.  
- The service can no longer derive the identifier held by an organisation.  

### 3.2 Frequent epoch rotation  
**Rejected** because:

- Epoch becomes a crude proxy for issuance date.  
- To implement expiry or corrections, the service must query organisations for **multiple epochs**.  
- This leads to performance degradation, unnecessary key churn, and operational instability.  
- Bulk‑issued identifiers would all expire simultaneously.

### 3.3 “Versioned SUI”  
**Rejected as the primary mechanism** because:

- Still fails to provide *issuance time*.  
- Still cannot distinguish “before” vs “after” a correction event.  
- Still breaks lookup unless the service tries **all versions** for every organisation.

### 3.4 Full central registry mapping Id → SUI, Organisation, Metadata  
**Rejected** because:

- This becomes a centralised subject database.  
- Reintroduces SUI at rest.  
- Eliminates the privacy advantage of deterministic cryptographic identifiers.  
- Turns the identifier into a meaningless token that must always be looked up.


## 4. Decision

To support **expiry** and **invalidation** while preserving **deterministic identifiers**, the system will maintain a **minimal central issuance store** containing exactly:

```
Id, IssuedAt
```

Nothing else.

- **No SUI**  
- **No Organisation**  
- **No Epoch**  
- **No subject or domain metadata**  

### 4.1 Why this is necessary

Without knowing when an identifier was issued, the service **cannot**:

- Expire an identifier after a fixed period  
- Determine whether an identifier predates a correction event  
- Carry out safe “next‑touch” refresh  
- Apply rolling cryptographic migration policies  
- Ensure consistent behaviour across organisations  

### 4.2 Why this does not break determinism

- The identifier remains a pure function of `{Organisation, SUI, Epoch}`.  
- IssuedAt is **not** part of the identifier.  
- IssuedAt is stored once, the first time the ID is requested.  
- The service can still derive an organisation’s identifier deterministically at any time.

This is the **minimum possible state** that enables required lifecycle features.


## 5. Consequences

### 5.1 Positive
- Deterministic ID generation preserved  
- No SUI stored centrally  
- Supports age‑based expiry  
- Supports correction‑based invalidation  
- Enables clean epoch transitions  
- Can be implemented as a high‑performance key–value store  
- Small, privacy‑safe state footprint  

### 5.2 Negative
- Introduces minimal central state  
- Requires availability and durability of the issuance store  
- Needs retention and cleanup policy  

## 6. Implementation Notes

- The issuance store is keyed by the **opaque identifier string** exactly as stored by organisations.  
- `IssuedAt` is the timestamp (UTC) when the identifier was first generated.  
- Subsequent regeneration under the same `{Organisation, SUI, Epoch}` does not update IssuedAt.  
- Deleting an entry removes lifecycle knowledge for that ID.

