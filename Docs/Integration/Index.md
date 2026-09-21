# System integration documentation pack

This page lists the integration documentation we have or need, who it is for, and whether it should be public, draft-only or kept in private operational runbooks. The repository is the source of truth. Approved public material is published through the public documentation site; sensitive operational material is maintained in the private SharePoint runbook area.

## Audience and terminology

The pack uses these distinct roles:

- **System provider**: supplies and maintains an integrating system.
- **Direct integrator**: configures or builds the technical connection for one integrating system.
- **Consuming organisation**: uses an enabled integrating system. It does not need to be a local authority or share a system provider with another organisation.
- **Service-team operator**: supports assurance, enablement, ongoing operations and the service lifecycle.

**System integration** is the one-time technical implementation and validation for an integrating system, for the relevant release and environment.

**Organisation registration** is the repeatable service enablement of an individual consuming organisation. It can follow system integration or happen independently where the service model allows it.

These terms do not imply that a system provider represents multiple consuming organisations.

## Document inventory

Each item has one primary audience. `Confirmed guidance` is eligible for public publication once the public documentation site workflow approves it. `Draft - not for production use` is excluded from public publication. `Private operational procedure` is available only to authorised staff in SharePoint.

| Document | Primary audience | Status | Publication boundary | Authoritative location |
| --- | --- | --- | --- | --- |
| Service overview | Consuming organisation | Confirmed guidance | Public | Public documentation site, sourced from this repository; public page to be created through the publication workflow |
| Integration prerequisites | Direct integrator | Confirmed guidance | Public | Public documentation site, sourced from this repository; page to be created through the publication workflow |
| Accepted technical contracts | Direct integrator | Confirmed guidance | Public | Public documentation site, sourced from accepted repository contracts; no stable public API/authentication reference is recorded yet |
| Implementation and testing guidance | System provider | Confirmed guidance | Public | Public documentation site, sourced from this repository; guide to be created through the publication workflow |
| Organisation-registration requirements | Consuming organisation | Confirmed guidance | Public | Public documentation site, sourced from this repository; requirements await an agreed organisation-registration model |
| Support routes | Consuming organisation | Confirmed guidance | Public | Public documentation site, sourced from this repository; routes to be confirmed through the publication workflow |
| Lifecycle webhook contract v1 | System provider | Draft - not for production use | Draft, excluded from public publication | [Supplier lifecycle webhook contract v1](../Design/Notifications-Webhooks/SupplierLifecycle/V1/Index.md) |
| Other proposed technical contracts | Direct integrator | Draft - not for production use | Draft, excluded from public publication | Relevant repository design document until accepted and moved into the public publication workflow |
| Onboarding case records and assurance evidence | Service-team operator | Private operational procedure | Private SharePoint | Private SharePoint runbook area |
| Supplier- or organisation-specific configuration, credentials and secret procedures | Service-team operator | Private operational procedure | Private SharePoint | Private SharePoint runbook area |
| Incident and escalation runbooks, dashboards and support history | Service-team operator | Private operational procedure | Private SharePoint | Private SharePoint runbook area |

The inventory defines the pack and its boundaries; it does not create the future public guides or private runbooks listed above.

## For service-team operators

Service-team operators support assurance, enablement, ongoing operations and service lifecycle activities. Authorised staff should use the private SharePoint runbook area for case records, evidence, configuration, credentials, incident response, escalations, dashboards and support history. This public boundary intentionally does not expose those operational procedures or their contents.

## Publication dependencies

Before the corresponding material can be published or moved from Draft to Confirmed guidance, the service needs:

- an approved public documentation site and publication workflow;
- named content owners, a review cadence and a change-control process;
- a stable public Get an Identifier API and authentication reference;
- an agreed organisation-registration model covering assurance, configuration, support and lifecycle responsibilities; and
- access control and ownership for the private SharePoint runbook area.

Lifecycle notifications have additional dependencies: an agreed acknowledgement timeout, completed technical and information-governance reviews, and acceptance of both the contract and its operating model. Until then, lifecycle-webhook material remains `Draft - not for production use` and excluded from confirmed public guidance.
