workspace "SUI" "'Single unique identifier' is a proposed set of systems and standards to faciliate information sharing between child social care (CSC) systems. This workspace/repository contains software that demonstrates the viability of using the NHS number as the SUI and how data could be transferred between data owners nationally, to present this data as a single view of a child for the improved safeguarding of children." {

    !docs "./Architecture models"

    !adrs "./Architecture decisions/Systems landscape"

    model {
        searcher = person "Safeguarding Practioners" "Directly working with children and families to provide social care. e.g. social worker."

        singleViewAndTransfer = softwareSystem "Single View & Transfer" "Local Authority system providing a single view for a child.\n\n* Multiple instances, one per LA" {
            sv_web   = container "Single View Web App" "User-facing application.\n\nProvides a single view of consolidated records about a searched for child" "ASP.NET MVC/Razor, HTML, Browser"
            transfer_service   = container "Transfer Service" "Orchestrates Find, Fetch, Aggregation, Consolidation, and Storage." "HTTP, .NET"
            consolidated_data_store = container "Consolidated Data Store" "Stores the consolidated records of any searched for child." "SQL Database" "Database"
        }

        match = softwareSystem "MATCH Service" "Resolves an NHS number (SUI) from demographics" "External, API" {
        }

        fetch = softwareSystem "FETCH Service" "Retrieves record content from Custodians given a pointer" "External, API" {

        }

        pds   = softwareSystem "PDS" "Personal Demographics Service used to search for NHS numbers from demographics" "ExternalGrey, API"

        txma = softwareSystem "TxMA" "Central audit/event backplane"

        custodian = softwareSystem "Custodian Interface" "Custodian service provides manifest of records and access to record data." {
            laCms     = container "LA CMS"       "Custodian system" "HTTP API" "Custodian, ExternalGrey"
            social    = container "Social Care"  "Custodian system" "HTTP API" "Custodian, ExternalGrey"
            education = container "Education"    "Custodian system" "HTTP API" "Custodian, ExternalGrey"
            health    = container "Health"       "Custodian system" "HTTP API" "Custodian, ExternalGrey"
            tags "ExternalGrey"
        }

        find = softwareSystem "FIND Service" "Determines which Custodians hold records for an NHS number and returns pointers" "External, API" {
            !adrs "./Architecture decisions/System/Find"

            find_orchestration = container "FIND.Orchestration" "Entry point for FIND; coordinates Auth, Directory, CORE, Cache, PEP and Audit; returns policy-filtered pointers to callers." "C#, Azure Functions"
            find_auth          = container "FIND.Account.Auth" "Authenticates relying parties (tokens/credentials) and ensures they are registered to use FIND." "C#, Azure Functions"
            find_directory     = container "FIND.Account.Directory" "Directory of relying parties and custodians (organisation id, type, attributes, endpoints)." "C#, Azure Functions / Storage"
            find_policyconfig  = container "FIND.PolicyConfig" "Client for DSA/PolicyHead and compiled policy metadata (read-only configuration, no inline rule evaluation)." "C#, Azure Functions"
            find_core          = container "FIND.CORE" "Executes new searches: builds criteria, selects custodians, calls CustodianWrapper, assembles candidate pointers." "C#, Azure Functions"
            find_wrapper       = container "FIND.CustodianWrapper" "CRI-style abstraction over custodian integrations; handles auth, protocol and mapping to canonical pointers." "C#, Azure Functions"
            find_pep           = container "FIND.PEP" "Policy Enforcement Point: applies compiled DSA policy to candidate pointers (mode = EXISTENCE) using in-memory policy artefact." "C#, Azure Functions / In-memory engine"
            find_cache         = container "FIND.Cache" "Caches FIND results (pointers) keyed by subject, relying party, purpose and policyVersionId." "Redis or similar"
            find_audit         = container "FIND.Audit" "Publishes structured audit and telemetry events from FIND components to TxMA." "C#, Azure Functions / Event stream"

            find_orchestration -> find_auth         "Authenticate relying party"
            find_auth          -> find_directory    "Lookup relying party organisation details"

            find_orchestration -> find_cache        "Check/store cached pointers"
            find_orchestration -> find_core         "Request new search when cache is missing or stale"

            find_core      -> find_directory        "Resolve which custodians are in scope"
            find_core      -> find_wrapper          "Use connectors to query custodians for manifests"

            find_wrapper   -> laCms                 "Discovery + manifest (pointers)" "HTTPS"
            find_wrapper   -> social                "Discovery + manifest (pointers)" "HTTPS"
            find_wrapper   -> education             "Discovery + manifest (pointers)" "HTTPS"
            find_wrapper   -> health                "Discovery + manifest (pointers)" "HTTPS"

            find_orchestration -> find_pep          "Submit candidate pointers for DSA evaluation (mode=EXISTENCE)"
            find_pep           -> find_policyconfig "Load PolicyHead / compiled policy metadata"
            find_pep           -> find_audit        "Emit policy decision events (with policyVersionId)"

            find_core          -> find_audit        "Emit search execution events"
            find_orchestration -> find_audit        "Emit request lifecycle events"
            find_cache         -> find_audit        "Emit cache usage events when relevant"
        }

        searcher -> sv_web "1. Uses" "HTTPS"
        sv_web   -> transfer_service "2a. Calls Transfer Service with a SUI" "HTTPS"
        transfer_service   -> consolidated_data_store "2b. Reads/writes consolidated records" "SQL"

        transfer_service -> match "3. Resolve SUI (request/response)" "HTTPS"
        match  -> pds   "4. Search NHS number from demographics (request/response)" "HTTPS"

        transfer_service -> find "5. Start discovery; poll status (request/response)" "HTTPS"
        find   -> laCms     "6a. Discovery + manifest (pointers)" "HTTPS"
        find   -> social    "6b. Discovery + manifest (pointers)" "HTTPS"
        find   -> education "6c. Discovery + manifest (pointers)" "HTTPS"
        find   -> health    "6d. Discovery + manifest (pointers)" "HTTPS"

        transfer_service -> fetch "7. Record by pointer (request/response)" "HTTPS"
        fetch  -> laCms     "8a. Retrieve record content (request/response)" "HTTPS"
        fetch  -> social    "8b. Retrieve record content (request/response)" "HTTPS"
        fetch  -> education "8c. Retrieve record content (request/response)" "HTTPS"
        fetch  -> health    "8d. Retrieve record content (request/response)" "HTTPS"
    }

    views {
        properties {
            "structurizr.sort" "created"
        }

        systemLandscape SUI "SUI systems landscape" {
            include searcher
            include singleViewAndTransfer
            include match
            include pds
            include find
            include fetch
            include custodian
            autoLayout lr
        }

        container singleViewAndTransfer "singleViewAndTransfer_Containers" {
            title "Single View & Transfer – Containers"
            include sv_web
            include transfer_service
            include consolidated_data_store
            autoLayout lr
        }

         container find "FIND_Containers" {
            title "FIND Service – Containers"
            include find_orchestration
            include find_auth
            include find_directory
            include find_policyconfig
            include find_core
            include find_wrapper
            include find_pep
            include find_cache
            include find_audit
            include laCms
            include social
            include education
            include health
            include txma
            autoLayout lr
        }

        styles {
            element "Person" {
                background #08427B
                color #ffffff
                shape Person
            }
            element "Software System" {
                background #1168BD
                color #ffffff
            }
            element "Container" {
                background #3C91E6
                color #ffffff
            }
            element "Database" {
                shape Cylinder
            }
            element "ExternalGrey" {
                background #9AA0A6
                color #ffffff
            }
            element "Decision:Draft" {
                colour white
                background #b8af5c
            }
        }
    }
}
