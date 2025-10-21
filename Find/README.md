# Find Service

The **Find** service matches persons to NHS numbers using the NHS FHIR API, with a custom algorithm to determine the
best match.

## Purpose

- Accepts person details (name, birth date, etc.)
- Queries the NHS FHIR API for possible matches
- Applies a custom algorithm to score and classify results:
    - *Match*: High-confidence match (score ≥ 0.95)
    - *Potential Match*: Medium-confidence (score ≥ 0.85 and < 0.95)
    - *Many Match*: Multiple possible matches found
    - *No Match*: No suitable match found
- Returns the best result according to defined priority rules

## Technology

- *.NET* SDK - use the latest version of .Net SDK 9
- *.NET* (C#)
- *Clean Architecture* principles
- *NHS FHIR API* integration

## Structure

- *Presentation Layer*: API endpoints (TBA)
- *Application Layer*: Matching logic and service interfaces
- *Domain Layer*: Core models and business rules
- *Infrastructure Layer*: FHIR API communication

## How It Works

1. Receives a person specification.
2. Builds search queries for the FHIR API.
3. Executes queries and evaluates results using the custom algorithm.
4. Returns the best match result, including NHS number if found.

---

For more details, see the code and tests in the repository.
