# Option A — Custodians Notify the Service of the Records They Hold (Central Link Table)

Option A provides a clear and simple approach in which custodians *tell* the SUI National Service about the records they hold, and the central service maintains a link table that records these associations.  
This allows the FIND service to determine which organisations hold records for a person **without needing to fan out queries** during a search, because the central service already knows who has notified data for that SUI.

### Enrolment

Organisations enrol with the SUI National Service, registering their endpoints and organisational details.  
No encryption keys are required under this model.

### Notifying the Existence of Records

Whenever a custodian creates or updates a record in their own system, they submit:

- A demographic record  
- Their internal record identifier  
- The type of record they hold  

The NOTIFY service resolves the demographic information via the Authoritative Source (PDS).  
If the demographic successfully resolves to an SUI (e.g., NHS number), a row is added to the central link table.

### Structure of the Link Table

Each row in the link table associates a custodian’s internal record with the SUI that person belongs to:

| Organisation Id | Record Type   | Record Id | SUI        |
|-----------------|---------------|-----------|------------|
| A               | Social Care   | XYZ123    | 0129384756 |
| B               | Crime         | 123123    | 0129384756 |
| C               | Housing       | ABCDEF    | 0129384756 |

Over time, this produces a complete picture of which organisations hold information about a given person.

### Searching for Records

Searchers can initiate a search in two ways:

#### 1. Search Using Demographic Data  
The FIND service uses the demographic record to obtain an SUI from PDS.  
Once the SUI is determined, the central link table is queried to locate all custodians who have previously notified data for that SUI.

#### 2. Search Using an Internal Record ID  
If the searcher already holds an internal record identifier for one of their own records, FIND can use that identifier to look up the SUI directly from the link table.  
This avoids the need for a demographic lookup.

### Manifest Generation and Retrieval

Once the SUI is known and the link table has been queried:

1. FIND identifies all custodians who hold relevant records.
2. Data Sharing Agreement (DSA) rules are applied to filter any records the searcher is not permitted to discover.
3. A manifest is generated, showing the allowed record types and custodian endpoints.
4. FETCH URLs are created to allow the searcher to retrieve record content securely.

Record retrieval through FETCH works in the same way as Option B: FIND mediates access to each custodian, applies policy-based filtering, and returns only the permitted content.

```mermaid
sequenceDiagram
    autonumber

    participant Org as Organisation (Searcher/Custodian)

    box rgba(240,240,240,0.5) "SUI National Service"
        participant SUI as SUI Onboarding
        participant Notify as NOTIFY service
        participant Find as FIND service
        participant Fetch as FETCH service
        participant Index as Link table / SUI index
    end

    participant Auth as Authoritative source (PDS)
    participant C1 as Custodian A
    participant C2 as Custodian B

    %% ONBOARDING – ORG REGISTRATION

    rect rgba(230,240,255,0.5)
    Note over Org,SUI: Onboard organisation with SUI National Service

    Org->>SUI: Register organisation, roles and endpoints
    SUI-->>Org: Registration confirmed
    SUI->>SUI: Store org profile and unique org identifier
    end

    %% ENROLMENT – NOTIFY RECORD EXISTENCE

    rect rgba(235,255,235,0.5)
    Note over Org,Notify: Notify existence of records via SUI National Service
    Note over Notify,Index: Link table row = { org id, internal record id, record type, SUI }

    loop For each eligible local record
        Org->>Notify: Notify record (demographics, record type, internal record id)
        Notify->>Auth: Lookup SUI for person
        Auth-->>Notify: SUI found or not found

        alt SUI found
            Notify->>Index: Upsert mapping (org id, internal record id, record type, SUI)
            Index-->>Notify: Link table updated
        else No SUI found
            Notify-->>Org: No match / error outcome
        end
    end
    end

    %% SEARCH – DISCOVER WHICH CUSTODIANS HOLD RECORDS

    rect rgba(240,255,240,0.5)
    Note over Org,Find: Search For a Person (via SUI National Service)

    alt Search using demographic record
        Org->>Find: Start discovery with demographic record
        Find->>Auth: Lookup SUI via PDS
        Auth-->>Find: SUI found or not found

        alt SUI found
            Find->>Index: Query link table by SUI
            Index-->>Find: Organisations, record types and endpoints
        else No SUI found
            Find-->>Org: No matching person found
        end
    else Search using internal record id
        Org->>Find: Start discovery with internal record id
        Find->>Index: Lookup SUI by org id and internal record id
        Index-->>Find: SUI found or not found

        alt SUI found
            Find->>Index: Query link table by SUI
            Index-->>Find: Organisations, record types and endpoints
        else No SUI found
            Find-->>Org: No matching link found
        end
    end

    Find->>Find: Apply data-sharing rules (filter disallowed records)
    Find->>Fetch: Register allowed manifests and generate Fetch URLs
    Fetch-->>Find: Searcher-specific, non-shareable, expiring URLs
    Find-->>Org: Discovery result (organisations, record types and Fetch URLs)
    end

    %% FETCH – RETRIEVE RECORD CONTENT

    rect rgba(255,245,235,0.5)
    Note over Org,Fetch: Retrieve record content via SUI National Service

    Org->>Fetch: Call Fetch URL for specific record
    Fetch->>Find: Resolve Fetch URL to custodian endpoint
    Find->>C1: Request record content for identifier
    C1-->>Find: Record content payload

    Find->>Find: Apply content-level policy (mask or remove disallowed content)
    Find-->>Fetch: Filtered record content and custodian contact details (if allowed)
    Fetch-->>Org: Return record content to searcher
    end
```
