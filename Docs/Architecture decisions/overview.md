# Overview

Architectural decisions in the single unique identifier (SUI) programme are 
captured using architecture decision records (ADRs).

Since many decisions will need to be recorded over multiple systems and components, the 
ADRs are structured into categories:

- **Systems landscape**: Decisions affecting all of the systems as a whole.

- **System**: Decisions on how particular systems are structured.

- **Component**: Decisions how particular components are structured.

## Summary of ADRs

### Systems landscape

| ID | Name |
|----|------|
| [ADR-SUI-0001](Systems%20landscape/0001-build-central-locator-and-la-generator.md) | Build a central locator service and generate single-view data sets in local authorities |
| [ADR-SUI-0002](Systems%20landscape/0002-create-new-component-for-central-matching-service.md) | Create a new component to implement a centralised NHS number matching service |
| [ADR-SUI-0003](Systems%20landscape/0003-testing-reqnroll-playwright.md) | Use ReqNRoll and Playwright to write and implement system tests |
| [ADR-SUI-0004](Systems%20landscape/0004-MockingTool.md) | Mocking Tool |
| [ADR-SUI-0005](Systems%20landscape/0005-compiled-dsa-policy-and-pep.md) | Compiled DSA Policy & Policy Enforcement Point (PEP) for FIND-A-RECORD and FETCH-A-RECORD |

### System

#### Find

| ID | Name |
|----|------|
| [ADR-FIND-0001](System/Find/0001-adopt-clean-architecture-find.md) | Adopt Clean Architecture for Find |
