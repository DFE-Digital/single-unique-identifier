# ADR-GetAnIdentifier-0001: MNS Integration

Date: 2026-08-24

Author: Stuart Maskell

Category: System/GetAnIdentifier / NHS Integration

## Status

Draft

## Decision

TBA

## Context

FHIR = NHS API for http communication with NHS systems.
MNS = Multicast Notification Service, a service that allows NHS systems to send notifications to subscribed systems when there is a change in a person's demographics or NHS number. It can as of August 2026 only push notifications to MESH or AWS SQS.
MESH = Message Exchange for Social Care and Health, a secure messaging service that allows NHS systems to send and receive messages.
AWS SQS = Amazon Web Services Simple Queue Service, a fully managed message queuing service.

As part of the Get an Identifier service, we will be 'subscribing' person(s) through FHIR to MNS, we will then need the NHS Multicast notification service to be able to receive notifications of when a person has had a change in GP registration or NHS number. This ADR is specifically around the technology of receiving notifications from MNS. As this is an Alpha, we must consider what will give us the notificaiton ability with the least resistnace and fastest time to live, while also considering the long-term maintainability of the solution.

## Options considered

1. AWS SQS
2. MESH
3. No integration

Our Get an Identifier application is a third-party software system hosted entirely within Microsoft Azure.

Because our system resides outside the NHS network and runs on Azure, we do not have native access to AWS infrastructure. We must establish an integration path that balances time to live against long-term maintainability, clear operational boundaries, and security.

Two primary integration paths were identified:

1. Adopting **NHS MESH**, the official, vendor-neutral NHS standard for asynchronous data transfer.
2. Requesting the NHS to provision a custom **AWS SQS queue** within their cloud environment, using either a cross-cloud push mechanism (via AWS Lambda pushing to Azure Service Bus) or a pull mechanism (Azure consuming SQS directly).

We need to decide on the integration standard to establish our infrastructure setup and team delivery roadmap.

## Consequences

### Option 1: NHS MESH

- **Positive:** Standardized, well-documented, official NHS integration route supported by standard operational governance.
- **Positive:** Clear operational boundaries—NHS maintains the MESH mailbox infrastructure; we maintain our Azure MESH client reader.
- **Positive:** Cloud-agnostic API that eliminates cross-cloud IAM or bespoke network boundary setup between AWS and Azure.
- **Negative:** On boarding and mailbox setup processes are governed by NHS timelines, introducing an estimated ~1 month lead time before development and testing can complete.
- **Negative:** Requires our team to implement and maintain a MESH client polling mechanism inside Azure. We can poll as frequently as needed.
- **Negative (Security Operations):** Requires client certificate setup and ongoing certificate life cycle management (mTLS authentication). Expiration or improper rotation of NHS-issued certificates will break connectivity and requires monitoring and key-vault management in Azure.
- MESH docs:
  - <https://digital.nhs.uk/developer/api-catalogue/message-exchange-for-social-care-and-health-api#overview--mesh-api-pseudocode>
  - <https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service#post-/subscriptions>

### Option 2: NHS-Hosted AWS SQS Integration

- **Positive:** Potentially faster initial proof-of-concept kickoff if NHS teams can quickly provision AWS resources.
- **Positive:** As this is a subscription based model, it gives the ability of near-real-time notifications of changes to demographics and NHS number by pushing notifications.
- **Positive:** Leverages native pub/sub message queueing mechanics familiar to cloud developers.
- **Negative (Operational Risk):** Shared operational complexity—troubleshooting dead-letter queues (DLQ), dropped messages, or connectivity issues requires cross-organizational support tickets between NHS AWS admins and our Azure team.
- **Negative (Sub-option A - Existing vs. Dedicated NHS AWS Account):** Provisioning within an existing NHS AWS account poses potential noisy-neighbor and security policy constraints. Provisioning a dedicated AWS account increases NHS administrative overhead and setup delays.
- **Negative (Sub-option B - Push Pattern via Lambda):** Requires the NHS to maintain custom Lambda code to forward messages, while also storing and managing credentials to write directly into our Azure endpoints over the public internet.
- **Negative (Sub-option B - Pull Pattern via direct SQS access):** Requires our Azure services to authenticate cross-cloud using AWS IAM credentials or temporary tokens provided by the NHS, introducing additional key management overhead.
- **Negative:** High architectural risk of creating bespoke, non-standard NHS technical debt that is fragile to future NHS infrastructure updates.

- SQS docs for direct integration between AWS and Azure:
  - <https://www.nuget.org/packages/AWSSDK.SQS/4.0.2.5>
  - <https://aws.amazon.com/sdk-for-net/>

### Option 3: Do nothing (On-Demand Requests)

- **Positive:** Eliminates all event-driven or polling back end integration complexity, cross-cloud architecture, and third-party notification subscription setup for Alpha.
- **Negative (System Design):** Fails to test or validate asynchronous, event-driven data flows, which is a objective of the integration architecture.
- **Negative (API Load & Stale Data):** Requires external consumers or client systems to issue repetitive, polling-style API requests to check for state updates, creating unnecessary traffic and risk of stale data. There is an option for DfE to hold a 'time to update' style of letting the consumer know when to re-check, but this comes with it's own complexities and risks.

### Both SQS and MESH options can

- **Positive**: Handle high volumes of data throughput, far higher than we expect in Alpha.

---

## Advice

! TODO: Re-Review after updates

- NHSE Product Manager for MNS, 2026-08-18 - For near-real-time notifications, SQS is the best option. However, if that is not a requirement, then either is are equally valid. If your Azure application can call the MESH API directly (or use the MESH client) and batch delivery fits your workflow, MESH does seem like the simpler path to go down.
- Technical lead SUI, 2026-08-## - TBA
- Technical Architect SUI, 2026-08-## - TBA
