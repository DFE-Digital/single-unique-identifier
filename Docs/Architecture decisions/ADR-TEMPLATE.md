# ADRXXX: [Title]

- **Date**: [Date in YYYY-MM-DD format]
- **Author**: [Your full name, as the owner of this decision]
- **Status**: [Draft | Accepted | Superseded by ADRXXX]
- **Category**: [Directory of ADR i.e. System, Component/X]

## Decision

<!-- In a few sentences, describe the decision taken. -->

## Context

<!-- Describe the forces and circumstances that brought about this decision.
What needs to be decided and why? -->

## Options considered

<!-- Briefly describe each option considered as a numbered list, marking the
selected option with '(SELECTED)'. Consider whether a 'do nothing' option 
would be wise.

For example:
1. SQL Server
2. (SELECTED) Postgres
3. No database
-->

## Consequences

<!-- For each of the options above, describe positive and negative
consequences of selecting that option. Create a new section for each
option under a level-3 heading.

Remember a law of architecture: There are no solutions, only
trade-offs. Make sure to include any negative consequences of the
selected option. 

For example:

### Option 1: SQL Server

- Feature [X] in T-SQL helps to more easily implement NFR [Y].
- Microsoft locked-down product, could be harder to migrate from it in the future.

### Option 2: Postgres

- The JSON datatype could really help simplify lots of our schema.
- Fully open-source, more options for replatforming.

etc.

-->

## Advice

<!-- List of advice gathered to make this decision, including the names and
roles of advisors and the date each piece of advice was gathered.

To keep the list focused: remember to gather advice, not opinions. The 
difference is that advice picks an option and gives some justification for that
option. If a person has lots of opinions, consider working it into the context.

Before a decision is accepted, you are expected to gather advice from
people affected by the decision and (if available) experts in the area of the 
decision.

For example:

*Joanne Bloggs, Enterprise architect.*  I would recommend Postgres because we tend to use it in lots of our other components. So there would be fewer surprises if it needed to be maintained by another team. We also have good relationships with the Postgres community.

*Jane Doe, Developer.* I would prefer SQL Server because it has great documentation which would speed up our implementation — making us deliver sooner.
-->
