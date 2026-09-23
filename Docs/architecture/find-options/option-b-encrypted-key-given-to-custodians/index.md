# Option B — Encrypted, Custodian-Scoped Identifiers Issued via the MATCH Service

In Option B, custodians do not handle the SUI (e.g., NHS number) directly.  
Instead, each custodian receives a **uniquely encrypted version** of the SUI that only the SUI National Service can translate.  
This allows organisations to reference the same individual across multiple systems **without ever exposing the underlying NHS number**, and without creating a universal token that could be correlated across domains.

## Enrolment and Key Allocation

When an organisation enrols with the SUI National Service, it is issued a **unique encryption key**.  
This key is never shared with any other organisation and is known only to the SUI Service itself.

## Obtaining an Identifier for Local Records

Custodians submit demographic records to the MATCH service to resolve a person’s identity.  
If a match is found via the Authoritative Source (PDS), the MATCH service:

1. Retrieves the SUI (e.g., NHS number)  
2. Encrypts it using *that custodian’s own key*  
3. Returns the resulting **custodian-scoped identifier**  

The custodian stores this encrypted identifier in their local database.  
They never store or see the SUI itself.

## Why Custodian-Scoped Encryption Matters

Because every custodian receives a **different encrypted representation** of the same SUI, the encrypted identifier:

- Cannot be correlated across organisations  
- Cannot be used as a cross-domain join key  
- Never becomes “sensitive” in the way a shared universal identifier would  

Even if a custodian widely reused or shared their local encrypted ID internally, it **would not behave like an NHS number** and would not increase linkage or privacy risk.  
Only the SUI National Service can translate between custodian-scoped identifiers.

## Searching for Records

A searcher initiates a search using *their own encrypted identifier* for a person.

The FIND service:

1. **Decrypts** the searcher’s identifier using the searcher’s key  
2. **Re-encrypts** the underlying SUI with every other custodian’s key  
3. Fans out queries to custodians using those custodian-specific encrypted values  

Each custodian can then determine whether they hold records for that person based on the identifier that *only they* can recognise.

## Discovery Results and Retrieval

Once custodians respond with either *“no records”* or a list of available record types, FIND:

- Applies DSA rules to filter out records the searcher is not permitted to discover  
- Constructs searcher-specific Fetch URLs  
- Returns a manifest showing which organisations hold which types of records  

The searcher then retrieves individual records from the FETCH service, which mediates access and applies any required content-level filtering or masking.

## Summary

Option B provides full cross-organisational discovery and record retrieval **without ever exposing the SUI**, and without the privacy or correlation risks associated with clear identifiers or shared tokens.  
The SUI is always encrypted in a custodian-specific way, and only the central service is able to translate between forms.

```mermaid
%%{init: {
  "theme": "default",
  "themeVariables": {
    "background": "#ffffff",
    "textColor": "#000000"
  }
}}%%
sequenceDiagram
    autonumber

    participant Org as Organisation (Searcher/Custodian)

    box rgba(240,240,240,0.5) "SUI National Service"
        participant SUI as SUI Onboarding
        participant Match as MATCH service
        participant Find as FIND service
        participant Fetch as FETCH service
        participant Crypto as SUI Encryption
    end

    participant Auth as Authoritative source (PDS)
    participant C1 as Custodian A
    participant C2 as Custodian B

    %% ONBOARDING – ORG REGISTRATION

    rect rgba(230,240,255,0.5)
    Note over Org,SUI: Onboard organisation with SUI National Service

    Org->>SUI: Register organisation, roles and endpoints
    SUI-->>Org: Registration confirmed
    SUI->>SUI: Store organisation profile and unique encryption key
    end

    %% ENROLMENT – IDENTIFIER ISSUANCE

    rect rgba(235,255,235,0.5)
    Note over Org,Match: Issue identifiers for local records via SUI National Service

    loop For each eligible local record
        Org->>Match: Send demographic record (name, gender, DOB, address)
        Match->>Auth: Lookup SUI for person
        Auth-->>Match: SUI found or not found

        alt SUI found
            Match->>Crypto: Encrypt SUI with organisation key
            Crypto-->>Match: Custodian-scoped identifier
            Match-->>Org: Return custodian-scoped identifier
            Org->>Org: Store identifier against local record
        else No SUI found
            Match-->>Org: No match / error outcome
        end
    end
    end

    %% SEARCH – DISCOVER WHICH CUSTODIANS HOLD RECORDS

    rect rgba(240,255,240,0.5)
    Note over Org,Find: Search For a Person (via SUI National Service)

    alt Identifier already held locally
        Org->>Org: Read stored custodian-scoped identifier
    else No identifier held
        Org->>Match: Send demographic record to obtain identifier
        Match->>Auth: Lookup SUI
        Auth-->>Match: SUI (if found)

        alt SUI found
            Match->>Crypto: Encrypt SUI with organisation key
            Crypto-->>Match: Custodian-scoped identifier
            Match-->>Org: Return custodian-scoped identifier
            Org->>Org: Store identifier for future use
        else No SUI found
            Match-->>Org: No match / error outcome
        end
    end

    Org->>Find: Start discovery with custodian-scoped identifier

    %% Decrypt searcher's identifier, then re-encrypt for each custodian

    Find->>Crypto: Decrypt identifier with searcher key
    Crypto-->>Find: SUI for subject

    Find->>Crypto: Encrypt SUI with each custodian key
    Crypto-->>Find: Custodian-scoped identifiers for all custodians

    par Query Custodian A
        Find->>C1: Do you hold records for this identifier?
        C1-->>Find: No records / manifest (record types and URLs)
    and Query Custodian B
        Find->>C2: Do you hold records for this identifier?
        C2-->>Find: No records / manifest (record types and URLs)
    end

    Find->>Find: Apply data-sharing rules (filter disallowed records)
    Find->>Fetch: Register allowed manifests and generate Fetch URLs
    Fetch-->>Find: Searcher-specific, non-shareable, expiring URLs
    Find-->>Org: Discovery result (organisations + record types + Fetch URLs)
    end

    %% FETCH – RETRIEVE RECORD CONTENT

    rect rgba(255,245,235,0.5)
    Note over Org,Fetch: Retrieve record content via SUI National Service

    Org->>Fetch: Call Fetch URL for specific record
    Fetch->>Find: Resolve Fetch URL to custodian endpoint
    Find->>C1: Request record content for identifier
    C1-->>Find: Record content payload

    Find->>Find: Apply content-level policy (mask/remove disallowed content)
    Find-->>Fetch: Filtered record content + custodian contact details (if allowed)
    Fetch-->>Org: Return record content to searcher
    end

```

![Option B]](./option-b.svg)