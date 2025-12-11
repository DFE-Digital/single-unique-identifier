# ADR-SUI-0007: Cryptographic Scheme for Custodian-Scoped Identifiers

Date: 2025-12-08

Author: Simon Parsons

Category: Systems landscape

## Status

Draft

## Context

ADR-SUI-0007 established that each custodian will store a **custodian-scoped identifier** for every person they hold records about. This identifier is derived from a central **Single Unique Identifier (SUI)** — currently the NHS number, retrieved via PDS.

The agreed **Discovery / FIND** pattern is:

1. Any organisation may act as a **Searcher** and/or **Custodian**.  
2. Each organisation has its **own scoped identifier** for a person, derived from the SUI and that organisation’s key.  
3. A Searcher calls FIND with *its own scoped identifier*, not the SUI.  
4. FIND must:  
   - Take the Searcher’s identifier.  
   - Resolve it back to the **SUI**.  
   - Derive the identifiers used by other custodians for that same SUI.  
   - Call those custodians using their scoped identifiers.  
5. We **do not** want a central lookup table of `(Org, Identifier) → SUI`.

Therefore, the cryptographic scheme MUST:

- Allow FIND to go from **Searcher identifier → SUI → Custodian identifiers**.  
- Be **deterministic** for the same `(Org, SUI)` so that identifiers are stable.  
- Be **scoped per organisation**, so that the same SUI yields different identifiers in different organisations.  
- Never embed demographics directly in the crypto; demographics are only used to resolve the SUI.  
- Produce **alphanumeric, fixed-length identifiers** that are safe for use in URLs, logs, and database keys.  
- Avoid a central lookup table for correlation.  
- Keep TTL / lifecycle out of the core scheme (handled separately).

This ADR decides which cryptographic scheme we will use and describes the resulting identifier format and examples.



## Cryptographic Options Considered

### Option A — Reversible Symmetric Encryption (AES‑256)

**Approach**

- Each organisation is assigned a unique symmetric key, `K_org` (AES‑256).  
- For a given SUI, the organisation-scoped identifier is:

  `ID_org = AES256_Encrypt(K_org, Payload, deterministic_IV)`

- The payload is, at minimum, the SUI; it may also include versioning metadata (but not TTL — that is covered in ADR‑SUI-0007).  
- FIND holds **all keys** and can:
  - Decrypt the Searcher’s `ID_searcher` to recover the underlying SUI.  
  - Encrypt that SUI with each custodian’s key `K_custodian` to derive their `ID_custodian`.

**What it means**

- The identifier is effectively a **ciphertext** representation of the SUI, scoped to a single organisation.  
- Any organisation’s identifier can be turned back into the SUI by the national service (which holds the keys), but **not** by the organisation itself.

**Pros**

- Supports the exact discovery flow we designed:  
  `ID_searcher → SUI → IDs for other custodians`, with **no lookup table**.  
- **Deterministic** per `(Org, SUI)` when using a deterministic encryption configuration (e.g. AES‑SIV or a derived IV).  
- Uses standard, well‑understood cryptography (AES‑256) with excellent library support.  
- Already used in prototypes.  
- Clean separation of duties: only national services know keys and SUI; custodians only see scoped identifiers.

**Cons**

- If an organisation key `K_org` is compromised, that organisation’s identifiers can be decrypted to reveal SUI for its population. Scope of damage is still limited to that organisation.  
- Requires careful design of:
  - AES mode (e.g. deterministic AEAD or IV-derivation scheme).  
  - Encoding from binary to alphanumeric.  

**Decision impact**

- This option is **accepted** as the scheme for Alpha.  
- It is the only option that supports:
  - Searcher-only identifier input.  
  - No lookup table.  
  - Deterministic, reversible mapping per organisation.



### Option B — Keyed Cryptographic Hash (HMAC)

**Approach**

- For each organisation, we generate a secret HMAC key `K_org`.  
- Organisation-scoped identifier is computed as:

  `ID_org = EncodeAndTruncate( HMAC-SHA-256(K_org, SUI) )`

**What it means**

- The identifier is a **one-way pseudonym** of the SUI under that organisation’s key.  
- Even with `K_org`, you cannot algebraically invert the HMAC to recover SUI.

**Pros**

- Deterministic per `(Org, SUI)`.  
- Strong one‑way property: if identifiers leak, they cannot be turned back into SUI using maths alone.  
- No IV or padding to manage. Simple crypto to implement correctly.

**Cons**

- **Irreversible**: FIND cannot go from `ID_org` to SUI by calculation.  
- To support `ID_searcher → SUI → other IDs`, FIND would need a central mapping:  
  `(Org, ID_org) → SUI`.  
- That mapping would effectively be the **lookup table we explicitly do not want**.  
- The central mapping would become high‑value SUI‑bearing state with complex governance and lifecycle.

**Decision impact**

- HMAC is attractive purely as a pseudonymisation mechanism, but it **cannot support** the chosen discovery flow **without** a central lookup table.  
- Because we explicitly reject a lookup table, this option is **rejected for this design**, but noted as a potential future option if the architecture changed to always start from SUI.



### Option C — Format-Preserving Encryption (FPE)

**Approach**

- Use a format‑preserving encryption scheme (e.g. FF1/FF3) designed for numeric identifiers.  
- Treat SUI (NHS number) as a string of digits and encrypt it under a per‑organisation key.  
- Output is another digit string of the same length, which looks like a legitimate numeric ID.

**What it means**

- Scoped identifiers **look like NHS numbers** or other natural numeric identifiers, while actually being encrypted under per‑organisation keys.  
- FIND can decrypt them because it knows the keys, like the AES case.

**Pros**

- Deterministic per `(Org, SUI)`.  
- Reversible by FIND, so it supports the `ID_searcher → SUI → IDs for other custodians` flow.  
- Outputs are numeric, human‑friendly, and look familiar to existing systems.

**Cons**

- FPE schemes are more complex and less widely implemented than standard AES modes.  
- Security properties are more subtle and require specialist review.  
- Adds implementation risk and complexity without a strong need, given that we can already control format via encoding over AES ciphertext.

**Decision impact**

- Architecturally compatible and potentially attractive for some systems.  
- However, due to added complexity and limited incremental benefit over AES + encoding, FPE is considered a **future enhancement**, not an Alpha decision.



### Option D — Plain Hash or Global-Salt Hash of SUI

**Approach**

- Compute `Hash(SUI)` or `Hash(Salt || SUI)` using a standard hash function like SHA‑256.

**What it means**

- Identifiers are simple hash digests of the SUI (possibly with a global salt).  
- There is no key separation per organisation.

**Pros**

- Very easy to implement.  
- Deterministic for a given SUI (and salt).

**Cons**

- NHS numbers are drawn from a **finite, structured space** (roughly 1 billion possibilities).  
- With a hash digest and the knowledge of format, an attacker can perform a **brute‑force attack** over the entire NHS number space and recover SUI.  
- A global salt, once known, does not significantly mitigate this risk.  
- No per‑organisation scoping; all identifiers are immediately linkable across domains.

**Decision impact**

- Cryptographically **too weak** for this threat model.  
- Does not meet per‑organisation scoping and privacy requirements.  
- **Rejected.**



### Option E — Token-style Encryption with TTL / Claims

**Approach**

- Construct a structured token payload (e.g. JSON) containing SUI, TTL, claims, etc.  
- Encrypt/sign this payload and use it directly as the identifier (e.g. a compact JWE/JWT-like token).

**What it means**

- The identifier doubles as both a **linkage key** and a **token** carrying lifecycle or authorisation information.

**Pros**

- Built‑in TTL, version, and other claims.  
- Useful for **access tokens** or proof-of-authorisation flows.

**Cons**

- If TTL or other mutable fields are part of the identifier, then the identifier must **change** when those fields are updated.  
- That breaks the key property we need for correlation: the identifier must be stable for a given `(Org, SUI)` until we consciously regenerate it.  
- Conflates correlation identity with session/authorisation semantics.  
- Complexity is pushed into every place that stores or consumes the identifier.

**Decision impact**

- This design is more suitable for **access control** and **short‑lived session tokens**, not for **persistent person identifiers**.  
- TTL and lifecycle are instead handled in **ADR‑SUI-0007**, separate from the core identifier scheme.  
- This option is **rejected** for the custodian‑scoped identifier.


## Chosen Scheme

We choose **Option A — Reversible Symmetric Encryption (AES‑256)** as the core scheme for custodian‑scoped identifiers.

Key properties:

- Each organisation has a unique AES‑256 key `K_org`.  
- For a given SUI, the identifier is:

  `Ciphertext = AES256_Encrypt(K_org, Payload(SUI, Version), deterministic_IV)`  
  `Identifier = Base32_Encode(Ciphertext)`  
  `IdentifierWithCheck = Identifier + CheckChar(Identifier)`

- Deterministic for a given `(Org, SUI)` due to deterministic encryption configuration.  
- Reversible by FIND to recover SUI (no lookup table).  
- Different for each organisation due to different keys.  
- Encoded as an **alphanumeric, fixed-length**, readable string.  
- Optionally protected with a checksum character.

TTL and lifecycle are addressed separately in **ADR‑SUI-0008**.


## Identifier Format and Character Set

AES ciphertext is binary and must be encoded into a safe identifier.

Requirements for the encoded identifier:

- **ASCII alphanumeric only** (no punctuation, no `+`, `/`, `=`, or whitespace).  
- **Fixed length** to simplify storage, indexing and validation.  
- Uppercase letters only.  
- Safe to appear in URLs, logs, HTTP headers, database keys.  
- Not easily confused visually.

We therefore:

- Use **Crockford Base32** for encoding:
  - Alphabet: `0–9 A–Z` minus `O, I, L, U` to avoid confusion.  
  - Case insensitive, but we standardise on uppercase.  
- Target a canonical length in the range **16–24 characters**, depending on ciphertext length and truncation decisions.

Presentation:

- **Canonical form**: continuous Base32 string, e.g. `B7Q6D9M2K4Z18H0R`.  
- **UI form** (optional): grouped for readability, e.g. `B7Q6-D9M2-K4Z1-8H0R`.

The canonical value is used for storage and APIs; grouping is purely visual.



## Worked Examples (Illustrative Only)

These examples are **fabricated** and for explanatory purposes only. They do not reflect real keys or algorithms.

Example SUI (NHS number as digits):

```text
9434765919
```

Organisation A:

- Key: `K_A` (not shown)  
- Payload: `{ SUI: 9434765919, V: 1 }`  
- Ciphertext (binary): `0x3A F1 97 4C 8D 22 9A 41 6E 01 53 2F 99 3D 8B 67`  
- Encoded canonical identifier (Base32):  

  ```text
  B7Q6D9M2K4Z18H0R
  ```

Organisation B:

- Key: `K_B` (different from `K_A`)  
- Same SUI and version.  
- Different ciphertext and encoded identifier, e.g.:  

  ```text
  P3X8N2T7J9C41V5Q
  ```

We see:

- Same SUI → different identifiers for different organisations.  
- Identifiers are fixed-length, alphanumeric, and transport‑safe.



## Visual Comparison of Methods (Illustrative Outputs)

Using the same example SUI `9434765919`:

| Method | Example Output | Deterministic? | Reversible to SUI (without lookup)? | Alphanumeric only? | Notes |
|--|-|-|-|--|-|
| AES‑256 raw | `0x3A F1 97 4C …` | ✓ | ✓ | ✗ | Binary; must be encoded |
| AES‑256 → Base64 | `OvGXTI0imkFuAVMv mT2LZw==` | ✓ | ✓ | ⚠ | Includes `+`, `/`, `=`; awkward in URLs |
| **AES‑256 → Base32 (Crockford)** | **`B7Q6D9M2K4Z18H0R`** | ✓ | ✓ | ✓ | **Chosen scheme** |
| AES‑256 → Base32 (grouped UI) | `B7Q6-D9M2-K4Z1-8H0R` | ✓ | ✓ | ✓ | Presentation only |
| HMAC → Base32 | `J1M8D6K2R9Q0F3W5T7C1` | ✓ | ✗ | ✓ | Would require lookup; rejected |
| FPE-FF1 (digits) | `5738912460` | ✓ | ✓ | ✓ | Same format as NHS-like number; not chosen for Alpha |

These outputs are indicative of **shape and properties**, not of actual production values.

## Optional Checksum Character

Because the identifier is an encoded ciphertext, **any change in any character** will decrypt to a completely different payload and SUI. Without a guard, this can cause:

- Silent failures where a mistyped ID appears valid to the system but no matching record is found.  
- Subtle and hard‑to‑debug discrepancies in discovery behaviour.

To mitigate this, we recommend adding a **final checksum character**, calculated over the encoded identifier.

Benefits:

- Detects single‑character errors.  
- Detects many transposition errors.  
- Prevents silent mismatches; bad identifiers are rejected early.  
- Does not affect cryptographic properties or reversibility.

Recommended algorithms:

- **Damm** or **Verhoeff**, both of which are well‑studied for error detection in numeric identifiers.

Structure:

```text
<EncodedIdentifier><CheckChar>
```

Example:

```text
Canonical encoded identifier:  B7Q6D9M2K4Z18H0R
With checksum:                B7Q6D9M2K4Z18H0RX
```

The checksum is optional but **recommended** for production.


## Decision

We adopt the following scheme for custodian-scoped identifiers:

- Use **AES‑256** reversible symmetric encryption with **per‑organisation keys**.  
- Use a **deterministic encryption configuration** (mode + IV derivation) so that identifiers are stable for a given `(Org, SUI, Version)`.  
- Encode ciphertext using **Crockford Base32**, yielding an alphanumeric, fixed-length canonical identifier.  
- Optionally append a **checksum character** to detect transmission and entry errors.  
- Exclude TTL and lifecycle logic from the identifier; these are handled separately in **ADR‑SUI-0007**.

Formula (conceptual):

```text
Payload      = { SUI, Version }
Ciphertext   = AES256_Encrypt(K_org, Payload, deterministic_IV)
Identifier   = Base32_Encode(Ciphertext)
FinalId      = Identifier + CheckChar(Identifier)   (recommended)
```

This scheme satisfies:

- No central `(Org, Identifier) → SUI` lookup.  
- Searcher-only identifier input to FIND.  
- Deterministic, reversible mapping per organisation.  
- Per‑organisation scoping.  
- Alphanumeric, URL‑safe, fixed‑length identifiers.


## Consequences

- A central **key registry** is required, holding one AES‑256 key per organisation (`K_org`).  
- Only trusted national services (MATCH, FIND) have access to keys and SUI.  
- Custodians and Searchers store and exchange only scoped identifiers, not SUI.  
- All services relying on these identifiers must share:
  - The same AES mode and deterministic IV derivation approach.  
  - The same Base32 encoding configuration.  
  - The same checksum algorithm, if used.  
- Key rotation, TTL hints, and identifier refresh strategies are defined in **ADR‑SUI-0007** and subsequent lifecycle ADRs.


## Related Decisions
- **ADR‑SUI-0006** — Custodian-Scoped Encrypted Identifier for Person-Level Correlation
- **ADR‑SUI-0008** — Identifier Lifecycle & TTL‑Driven Refresh Strategy (TTL & rotation)  
