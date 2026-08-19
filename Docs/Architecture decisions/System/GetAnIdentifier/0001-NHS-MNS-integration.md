# ADR-GetAnIdentifier-0001: MNS Integration

Date: 2026-08-18

Author: Stuart Maskell

Category: System/GetAnIdentifier / NHS Integration

## Status

Accepted

## Decision

MESH has been chosen due to having the least technical overhead and is the recommended path by NHS. MESH has a path to live process which includes development, testing and live. SQS would need to us create each one individually.

## Context

FHIR = NHS API for http communication with NHS systems.
MNS = Multicast Notification Service, a service that allows NHS systems to send notifications to subscribed systems when there is a change in a person's demographics or NHS number.

As part of the Get an Identifier service, we will be 'subscribing' person(s) through FHIR to MNS, we will then need the NHS Multicast notification service to be able to receive notifications of when a person has had a change in GP registration or NHS number. This ADR is specifically around the technology of receiving notifications from MNS.

There is a choice between only 2 options the NHS has to send notifications to:

- SQS
- MESH

## Options considered

1. AWS SQS
2. MESH (SELECTED)

## Consequences

### Option 1: AWS SQS

- Gives the ability to have near-real-time notifications of changes to demographics and NHS number by pushing notifications.
- This does require 3 parts of additional infrastructure - SQS queue, Lambda function and Azure Service bus (or similar messaging system). There are some libraries that can connect to SQS but this would require additional authentication measures.
- SQS can handle very high volumes of messages, far higher than that which we expect.
- High throughput with notification delivery in near-real-time, however this is not a requirement for the Get An Identifier service.

### Option 2: MESH

- Gives the ability to follow the standard path to live process that is well documented.
- Requires no additional infrastructure.
- MESH can handle high volumes of messages, far higher than that which we expect.
- We can poll MESH fairly frequently, recommended is between 10-60 minutes on the low end and that is based on other users of MESH, however there is no lower limit and we can be more aggressive.
- Unlike AWS SQS, MESH requires active polling, introducing processing latency (10-60 minutes) and requiring a scheduler/worker mechanism.
- Interfacing with MESH requires installing/managing the MESH Client or writing custom API integration logic.
- MESH docs:
  - <https://digital.nhs.uk/developer/api-catalogue/message-exchange-for-social-care-and-health-api#overview--mesh-api-pseudocode>
  - <https://digital.nhs.uk/developer/api-catalogue/multicast-notification-service#post-/subscriptions>

## Advice

- NHSE Product Manager for MNS, 2026-08-18 - For near-real-time notifications, SQS is the best option. However, if that is not a requirement, then either is are equally valid. If your Azure application can call the MESH API directly (or use the MESH client) and batch delivery fits your workflow, MESH does seem like the simpler path to go down.
- Brad Park, Technical lead SUI, 2026-08-19 - MESH seems the simpler choice. Less implementation complexity and meets requirements.
- Josh Taylor, Technical Architect SUI, 2026-08-19 - MESH seems like better choice, don't have to deal with multi-cloud. Can't see a need for instant notifications based on context. Polling integration can be made robust, if we keep queues on DfE-side.
