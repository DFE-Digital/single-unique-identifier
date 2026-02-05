# ADR-SUI-0011: Authentication and Trust Boundaries for SUI APIs

<<<<<<< HEAD
Date: January 2026  
=======
Date: 2026-01-28
>>>>>>> main
Author: Simon Parsons  
Decision owners: SUI Service Team (with input from DfE / participating custodians)  
Category: Security architecture

## Status
Draft — framing agreed, design decisions pending

---

## Context

The national SUI service provides discovery capabilities across multiple sovereign
data controllers (custodians). Consuming organisations (“searchers”) use the service to:

- **MATCH** a person and obtain the identifier used for discovery,
- **FIND** which custodians may hold records for that person,
- and subsequently **FETCH** record content, either directly from custodians or
  via agreed integration patterns.

The identifier used for discovery is the Single Unique Identifier (SUI), which in
the current architecture is the NHS number.

The service must operate correctly in both **attended** and **unattended** modes.
Because discovery is asynchronous and relies on polling, unattended operation is
unavoidable.

This ADR defines the **authentication and trust boundaries** across the discovery
workflow. It intentionally does **not** define detailed authorisation rules, policy
enforcement, or the final FETCH architecture, which are addressed in separate ADRs.

---

## Attended and unattended execution

In attended scenarios, a human user may be actively using an application that initiates
a search. In unattended scenarios, the system must continue to operate after the user
has left, for example while polling for discovery job completion or handling custodian
responses.

Because of this, authentication must not depend on continuous user presence.
The system must remain authorised to act on behalf of an organisation independently
of any individual user session.

---

## Boundary 1: End user → Searcher application

A human user authenticates to an application that integrates with the national discovery
service. This authentication is governed by the organisation operating the application
and is outside the direct control of the national service.

At this boundary:
- the user may authenticate via an organisation-specific identity provider,
- role-based access control may already be applied locally,
- and the national service may have no direct knowledge of the user or their credentials.

The discovery service must therefore not assume participation in, or control over,
this authentication step.

---

## Boundary 2: Searcher application → Discovery service (MATCH / FIND)

When a search is initiated, the consuming application must authenticate to the
national service in order to perform MATCH and FIND operations using the SUI.

This interaction must function in both attended and unattended modes. In particular:
- a user may initiate a search and then leave,
- discovery may continue asynchronously,
- polling for completion must continue without user involvement.

For this reason, **machine-to-machine authentication is a hard requirement** at this
boundary.

**Decision (locked):**

> All interactions between searcher applications and the national discovery service
> must be supportable using organisation-level, machine-to-machine authentication.

The open question at this boundary is whether the national service also needs awareness
of the **end user** and their **role**, in addition to authenticating the organisation.

Two models remain open:

- The national service authenticates only the organisation, and user-level access
  control is enforced entirely by the consuming application.
- The consuming application asserts user context to the national service, allowing
  central filtering or enforcement decisions.

No decision has yet been made between these models.

---

## Boundary 3: Custodian → Discovery service (polling and job handling)

Custodians authenticate to the national service in order to:
- poll for discovery jobs,
- receive work items,
- respond with manifests or confirmations,
- and signal completion or status.

These interactions are **always unattended**.

As such:
- machine-to-machine authentication is mandatory,
- credentials must be centrally manageable, rotatable, and revocable,
- and trust is established at the organisation-to-service level.

No user-level context is expected or required at this boundary.

---

## Boundary 4: Discovery service ↔ Custodian (polling, fan-out, and notifications)

In distributed discovery models, interaction between the national service and custodians
may occur in more than one mode.

The **primary and preferred mechanism** is **custodian-initiated polling**, where the
custodian authenticates to the national service to obtain discovery jobs and return
responses. This interaction is unattended and relies on machine-to-machine authentication.

In addition to polling, two other interaction patterns may apply:

### Service-initiated calls (fan-out)

In some architectures, the national service may initiate calls to custodians in order to:
- request confirmation of record existence for a given SUI,
- retrieve manifests,
- or perform other discovery-related operations.

Where the national service initiates these calls, it must authenticate directly to the
custodian. This implies that:
- custodians must provision credentials for the national service,
- the national service must securely store, rotate, and manage those credentials,
- and custodians must be able to audit and revoke access.

This pattern places a higher operational burden on both parties and is therefore not
assumed as the default.

### Webhook-based notifications (future)

An alternative to polling is a webhook-based model, where custodians register an endpoint
to receive notifications from the national service.

In this model:
- the custodian controls the endpoint and associated credentials,
- authentication and verification of incoming calls is configured by the custodian,
- and the national service authenticates itself when delivering notifications.

Webhook configuration implies the existence of a **custodian-facing administrative
interface**, where custodians can:
- authenticate interactively,
- manage endpoints and credentials,
- and control notification behaviour.

This interaction model is expected to be covered by a dedicated ADR.

### Authentication implications

Across all variants of this boundary:
- polling interactions require unattended, machine-to-machine authentication from
  custodian to national service,
- service-initiated calls require organisation-level authentication from national service
  to custodian,
- and webhook-based delivery shifts credential ownership and management to the custodian.

All interactions at this boundary are strictly organisation-to-organisation. No end-user
context is expected or required.

---

## Boundary 4a: Custodian administrator → National service portal (interactive administration)

In addition to machine-to-machine integration, the national service may need to provide an
interactive administrative portal for custodians.

This portal is distinct from any publicly available technical documentation. Its purpose
would be to support operational administration activities such as:

- onboarding and organisation profile management,
- obtaining or rotating credentials used for unattended machine-to-machine integration,
- configuring notification mechanisms such as webhooks (where supported),
- viewing integration health, status, and operational messages relevant to the custodian.

If this portal exists, it introduces another authentication boundary:

- a named individual acting on behalf of a custodian organisation authenticates to the
  national service portal using an interactive login,
- and is granted access based on organisational authority (for example, an administrator
  or technical owner role).

This portal may become the mechanism through which machine-to-machine credentials are
issued, rotated, or revoked on an organisation-by-organisation basis. It may also become
the mechanism by which custodians supply endpoint and authentication details for
service-initiated interactions such as webhooks.

No decision is recorded in this ADR on:
- the identity provider used for portal login,
- how administrator roles are proven and governed,
- or the specific credential issuance model.

The existence of a portal does not change the baseline requirement that production
integration with the national service must be supportable via machine-to-machine
authentication.

---

## Boundary 5: Manifest retrieval by searchers

When a searcher retrieves a manifest from the national service, the sensitivity of the
data increases.

At this point, the service is revealing **knowledge that records exist**, potentially
across multiple custodians, for a given SUI. This information is governed at least by:
- organisation-level Data Sharing Agreements (enforced via PEP),
- and potentially by additional constraints related to user role or purpose.

This is the first boundary at which **user-level authorisation** may become relevant.

The architecture must therefore support:
- organisation-level enforcement as a minimum,
- and the possibility of user-level filtering if required.

Whether that user-level logic is enforced centrally by the national service or delegated
to the consuming application remains an open decision.

---

## Boundary 6: Record access (FETCH)

Record access represents the highest sensitivity point in the system, as it involves
disclosure of record **content**, not just existence.

Two broad models are under consideration:

### Federated fetch

In a federated model:
- the national service returns pointers or references to custodian systems,
- the searcher (or end user) authenticates directly with the custodian,
- and the custodian enforces its own authentication and authorisation rules.

In this model, the national service does not need to authenticate end users for
record access.

### Proxied fetch

In a proxied model:
- the searcher requests record content via the national service,
- the national service authenticates to custodians and retrieves records,
- and the national service returns record content to the searcher.

This model significantly increases the authentication, authorisation, and governance
responsibilities of the national service.

No decision has yet been made on which model (or hybrid) will be adopted.

---

## Technology considerations (non-decisional)

The following technologies may be relevant to future design decisions.

### Unattended / machine-to-machine
- OAuth 2.0 client credentials
- Managed identities / service principals
- Short-lived bearer tokens
- mTLS, where feasible

Static or shared API keys are explicitly out of scope.

### Attended / user authentication (if required)
- Entra ID / Azure AD federation
- DfE Sign-in / DfE ID
- Federated SSO using OIDC or SAML
- Cross-government identity solutions (subject to policy)
- NHS Login where healthcare-specific access is appropriate

No assumption is made that a single identity provider will be suitable for all
participants.

---

## Decision

The following decision is recorded:

> Machine-to-machine authentication is mandatory and sufficient as a baseline for all
> interactions with the national discovery service, including attended and unattended
> operation.

The following decisions are explicitly deferred:
- whether the national service must process end-user identity,
- whether role-based access control is enforced centrally or delegated,
- and which FETCH trust model will be adopted.

---

## Consequences

### Positive
- Supports asynchronous and polling-based discovery.
- Minimises coupling to user identity frameworks.
- Enables incremental onboarding of organisations.
- Keeps authentication stable as interaction models evolve.

### Trade-offs
- Defers decisions on centralised user-level enforcement.
- Requires consuming applications to handle user-level access control if delegated.
- Leaves some governance questions open pending DfE direction.

---

## Next steps

1. Confirm DfE expectations regarding identity and trust frameworks.
2. Determine whether user-level context is required centrally.
3. Decide on the preferred FETCH model.
4. Create follow-on ADRs covering:
   - Authorisation and purpose binding,
   - Record access (FETCH) architecture,
   - Credential lifecycle and operational controls.
