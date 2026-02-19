# Option C — SUI in the Clear

Option C operates in a very similar way to Option B, but without any encryption.  
Custodians work directly with the SUI (NHS number) rather than an encrypted identifier.

## Enrolment

Organisations still enrol with the SUI National Service, but no encryption key is allocated.

## Matching Records

Custodians continue to match their demographic records using the MATCH service, but instead of receiving an encrypted identifier, the MATCH service returns the SUI (NHS number) in the clear.

The custodian stores this SUI directly against their local person record in their own data store.

## Searching for a Person

When a searcher initiates a search, they do so using the SUI:

- If they already hold the SUI in their system, they pass it directly to the FIND service.
- If they do not hold the SUI, they can still obtain it by submitting a demographic record to the MATCH service.

Because the SUI is already provided in clear form, the FIND service does not need to decrypt anything.  
It simply uses the SUI as-is.

## Discovery

FIND fans out to all registered custodians to determine which organisations hold records for that SUI.  
Custodians respond with either:

- *No records found*, or  
- A manifest listing the record types they hold and the URLs for retrieving them.

FIND then:

1. Applies Data Sharing Agreement (DSA) rules to filter out any records that the searcher is not permitted to discover.
2. Wraps custodian endpoints behind searcher-specific Fetch URLs.
3. Returns the filtered manifest to the searcher.

## Fetching Records

The FETCH process is identical to Option B:

- The searcher calls the Fetch URL.
- FETCH resolves this to the custodian endpoint.
- The custodian returns the record content.
- FIND applies any content-level filtering required by policy.
- The filtered record is returned to the searcher.

The only material difference from Option B is the absence of encryption or decryption anywhere in the flow.


```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#ffffff",
    "primaryTextColor": "#000000",
    "secondaryTextColor": "#000000",
    "tertiaryTextColor": "#000000",
    "lineColor": "#000000"
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
    end

    participant Auth as Authoritative source (PDS)
    participant C1 as Custodian A
    participant C2 as Custodian B

    %% ONBOARDING – ORG REGISTRATION (NO KEYS ISSUED)

    rect rgba(230,240,255,0.5)
    Note over Org,SUI: Onboard organisation with SUI National Service (no encryption keys)

    Org->>SUI: Register organisation, roles and endpoints
    SUI-->>Org: Registration confirmed
    SUI->>SUI: Store organisation profile and identifier
    end

    %% ENROLMENT – STORE SUI IN CUSTODIAN DATA STORE

    rect rgba(235,255,235,0.5)
    Note over Org,Match: Match local records and store SUI in clear
    Note over Match,Org: MATCH returns SUI (e.g. NHS number) directly

    loop For each eligible local record
        Org->>Match: Send demographic record (name, gender, DOB, address)
        Match->>Auth: Lookup SUI for person
        Auth-->>Match: SUI found or not found

        alt SUI found
            Match-->>Org: Return SUI in clear
            Org->>Org: Store SUI against local record
        else No SUI found
            Match-->>Org: No match / error outcome
        end
    end
    end

    %% SEARCH – DISCOVER WHICH CUSTODIANS HOLD RECORDS

    rect rgba(240,255,240,0.5)
    Note over Org,Find: Search For a Person using SUI (no decryption needed)

    alt Searcher already holds SUI
        Org->>Org: Read stored SUI from local record
    else Searcher does not hold SUI
        Org->>Match: Send demographic record to obtain SUI
        Match->>Auth: Lookup SUI via PDS
        Auth-->>Match: SUI found or not found

        alt SUI found
            Match-->>Org: Return SUI in clear
            Org->>Org: Store SUI for future use
        else No SUI found
            Match-->>Org: No matching person found
        end
    end

    Org->>Find: Start discovery with SUI
    Note over Find,C1: FIND uses SUI directly (no decrypt / re-encrypt)

    par Query Custodian A
        Find->>C1: Do you hold records for this SUI?
        C1-->>Find: No records / manifest (record types and URLs)
    and Query Custodian B
        Find->>C2: Do you hold records for this SUI?
        C2-->>Find: No records / manifest (record types and URLs)
    end

    Find->>Find: Apply data-sharing rules (filter disallowed records)
    Find->>Fetch: Register allowed manifests and generate Fetch URLs
    Fetch-->>Find: Searcher-specific, non-shareable, expiring URLs
    Find-->>Org: Discovery result (orgs, record types and Fetch URLs)
    end

    %% FETCH – RETRIEVE RECORD CONTENT (SAME AS OPTION B)

    rect rgba(255,245,235,0.5)
    Note over Org,Fetch: Retrieve record content via SUI National Service

    Org->>Fetch: Call Fetch URL for specific record
    Fetch->>Find: Resolve Fetch URL to custodian endpoint
    Find->>C1: Request record content for SUI
    C1-->>Find: Record content payload

    Find->>Find: Apply content-level policy (mask or remove disallowed content)
    Find-->>Fetch: Filtered record content and custodian contact details (if allowed)
    Fetch-->>Org: Return record content to searcher
    end

```
