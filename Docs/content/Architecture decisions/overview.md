# Overview

Architectural decisions in the single unique identifier (SUI) programme are 
captured using architecture decision records (ADRs).

Since many decisions will need to be recorded over multiple components, the 
ADRs are structured into two categories:

- **System**: Decisions on how the system is structured as a whole.

- **Component**: Decisions how particular components are structured.

## Summary of ADRs

### System

| ID | Name |
|----|------|
| [ADR001](System/ADR001-build-central-locator-and-la-generator.md) | Build a central locator service and generate single-view data sets in local authorities |
| [ADR002](System/ADR002-create-new-component-for-central-matching-service.md) | Create a new component to implement a centralised NHS number matching service |

### Component

#### SystemTests

| ID | Name |
|----|------|
| [ADR001](Component/SystemTests/ADR001-testing-reqnroll-playwright.md) | Use ReqNRoll and Playwright to write and implement system tests |
