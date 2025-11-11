# ADR002: Create a new component to implement a centralised NHS number matching service

- **Date**:       2025-08-26
- **Author**:     Joshua Taylor
- **Status**:     Accepted
- **Category**:   System

## Decision

Create a new component to provide a centralised NHS number matching
capability for CSC data sources. Keep and maintain the Wigan 'SUI
matcher' as a separate component. Re-implement the matching algorithm
and FHIR integration in the new component, using lessons learned from
creating the 'SUI matcher'.

## Context

Between February and August 2025, the '[[SUI
matcher]{.underline}](https://github.com/DFE-Digital/SUI_Matcher)'
component was built out. The SUI team built it to ease the integration
burden for local authorities trying to retrieve NHS numbers from the
personal demographic service (PDS), the 'source of truth' for NHS
numbers. From this, the matching rates between child data held in a
local authority's CMS and NHS numbers could be measured. This
established whether the method of integration (FHIR API) would provide
better matching rates compared to as-is matching processes within LAs.

The technical vision of the SUI matcher is a set of cloud
infrastructure, including .NET APIs, which could be deployed into local
authority Azure clouds. This was structured as a .NET Aspire
application. From this, an integration client was created for the first
pilot local authority, Wigan council, which received datasets from their
case management system (CMS) and processed these with the SUI matcher
APIs.

In light of [[System
ADR001]{.underline}](./ADR001-build-central-locator-and-la-generator.md),
the matching capabilities of the SUI matcher are to be centralised to a
service provided by the DfE. This provoked a question for the SUI team:
to what extent should the work from the SUI matcher be reused for this
centralised matching service?

The resulting centralised matching service should ideally incorporate
any lessons learned through the building of the SUI matcher. The new
NFRs for the centralised matching service, which will understandably be
different from the 'SUI matcher', should also be considered.

The 'SUI matcher' may also need to be maintained for usage by Wigan, for
a currently undefined length of time.

## Options considered

1.  (SELECTED) Create a new component, only reusing the algorithm and
    integration logic. Continue supporting the 'SUI matcher' as a
    separate component.

2.  Duplicate SUI matcher code and refit for centralised NFRs,
    supporting separate code bases for different use cases.

3.  Refit existing SUI matcher code --- support for both use cases and
    their respective NFRs.

4.  Extract matching algorithm core logic from the 'SUI matcher' into a
    shared library. Create a new component dependent on this library.

## Consequences

### Option 1: New component, reuse logic

- Can incorporate lessons learned from building the 'SUI matcher' into
  the design of the centralised matching service. i.e. 'green field'
  benefits.

- Could increase total delivery cycle time, due to existing
  functionality being re-implemented.

- Could decrease total delivery cycle time, due to having less existing
  code to revisit and modify.

- No dependencies between the centralised service and SUI matcher.
  Allows code bases to evolve independently.

### Option 2: Duplicate & refit SUI matcher

- The algorithm, FHIR implementation and a starting point to the
  centralised API data model are already implemented --- could decrease
  total delivery cycle time.

- No dependencies between the centralised service and SUI matcher.
  Allows code bases to evolve independently.

### Option 3: Only refit SUI matcher and support both use cases

- Only one component needs to be maintained going forwards.

- Development work for the centralised use case would need to also be
  tested on for the Wigan use case. Overall higher testing and
  potentially development load --- increased total delivery cycle time.

### Option 4: Shared library

- Separate components for each use case can evolve independently, while
  keeping the matching algorithm consistent.

- Due to the total amount of components/libraries and their
  integrations, likely the most complex option to take forward --- both
  in total delivery cycle time and maintenance burden.

## Advice

*Ed Hiley, Tech Director, 2025-08-26.* It makes sense to take option 1,
because the two components are solving two different problems.

*Stuart Maskell, Developer, 2025-08-27.* Option 1 is my preferred
choice, since it gives lots of opportunity to make improvements ---
given what we've already learned. We should have enough time and people
to deliver this approach. The SUI matcher code would need to be
reworked/refactored anyway, so option 2 would add effort in any case.

*Dan Murray, Technical Architect, 2025-08-27.* Too many workarounds with
option 3, would have lots of configuration challenges to accommodate
both use cases. Option 4 adds lots of complexity. Both option 2 and 1
would work well. It depends on whether the developers would prefer to
continue using .NET Aspire for the central service. If yes, then option
2 would provide a good base.
