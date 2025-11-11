# ADR001: Build a central locator service and generate single-view data sets in local authorities  
  
- **Date**:     2025-08-15
- **Author**:   Josh Taylor
- **Status**:   Accepted
- **Category**: System

## Decision

We will build a central service for NHS number matching, data source
locating and integration services at the DfE. We will also provide an
installable system for LAs to use this central service and obtain data
relevant to their child population which will be used to generate and
access CSC single views.

## Context

The multi-agency information sharing (MAIS) programme has run several
pilots on how a single unique identifier (SUI) for child social care
(CSC) could help CSC practitioners within local authorities (LAs) to
carry out their social care work.

From these pilots we have learned that CSC records in local authority
case management systems are likely to match with an NHS number
successfully. However we have also learned that accessing the systems
required to retrieve NHS numbers can have many barriers for LAs, such as
an IG legal process which took 10 months at Wigan Council.

The next pilot to deliver, pilot 3, will explore approaches for getting
records linked together across government services using a SUI. From
this, the idea to build out a sandboxed version of a MAIS system came
about.

The high level structure of this system needed to be decided, so that
the idea of the pilot could be consistently explained to stakeholders
and initial backlogs of work could be created.

The system resulting structure needs to account for the functional
requirements of the system: accessing a single view for a child and .

It also needs to account for national opinions on data privacy.
Specifically by the precedent set by ContactPoint, a previous DfE system
built for this purpose which was cancelled in 2010.

## Options considered

1.  (SELECTED) Provide an installable system for LAs to generate and
    access single views; centralise NHS number matching, data source
    locating and integration services to the DfE.

2.  Centralise to the DfE all capabilities needed to create and serve a
    CSC single view (i.e. build ContactPoint again).

3.  Provide an installable system for LAs which directly integrate with
    their required data sources and NHSE PDS.

## Consequences

### Option 1: LAs consolidate, DfE facilitates

- The information governance process to access PDS, the source of NHS
  numbers, only needs to be done once for the DfE. The DfE then controls
  the process of access for LAs, which can be considerably faster since
  it is only being accessed for one use case --- speeding up adoption of
  the NHS number as a SUI.

- By bringing information locating capabilities into the DfE, the
  sharing of data sets across local authority boundaries can be
  facilitated to address boundary issues in current care processes ---
  without needing to store any personal data centrally, addressing
  central government data privacy concerns.

- Data source integration will need to be standardised across local
  authorities, their services and the DfE --- since information on those
  integrations will be stored centrally and the integrations ideally
  made automatically. Getting these standards right will be a risk area
  of work.

### Option 2: DfE takes on all capabilities

- Fewer overall integrations required. Data governance processes and
  integrations with data sources and the NHS only need to be completed
  once to provide CSC single views --- could be delivered sooner if
  integrations are delivery bottlenecks.

- Security risk is consistent across the country, since local
  authorities do not operate infrastructure.

- Likely the option with highest costs and responsibility for the DfE.
  This would be alleviated from the local authority.

- Resulting system would need to be highly available and reliable ---
  overall system could be more fragile. Hard to provide backup
  solutions.

- Creates a centralised database of child data causing:

  - data privacy concerns

  - central security risk, due to a honeypot effect; would need lots of
    investment in security

### Option 3: Installed system in LA takes on all capabilities

- Local authorities would need to bear the cost and effort of locating
  and integrating relevant data sources. This may be limited by
  resources available and low technical maturity.

- Without centralised locating services, boundary issues would still be
  difficult to solve across local authorities.

- Could lead to a divergence of some LAs using installed systems and
  others not --- no new value being provided to technically mature
  locally.

- Since no permanent service is being provided by the DfE, only
  components, there is a risk that components may not be maintained over
  time. Local authorities may also lag behind in maintenance.
