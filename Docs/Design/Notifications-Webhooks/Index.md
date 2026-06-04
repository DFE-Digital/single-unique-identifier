# Notifications (Webhooks) - Requirements and High-Level Design

**Date:** `2026-06-04`  
**Owner:** SUI Service Team  

This document outlines the key requirements, technical guidelines and high-level design for adding Notifications to the SUI system.


## Goals

The goals and reasons for adding Notifications to the SUI system are:

* Relive pressure that polling will put on the system.
* Ability for some amount of search (find-record) results to be provided more quickly.


## Requirements

1. Custodians should be able to be notified when they have jobs, rather than having to poll for jobs.
2. (Ideally) Searchers should be able to be notified when their search job completes, rather than having to poll for search results.


## Technical Guidelines / Decisions

* Notifications will be implemented as **Webhooks**.
* Subscribing to Webhooks should be **optional**, so that Polling is still offered as the baseline integration.
* The solution must **build upon Jobs**, so that:
  - There is a single mechanism for Custodians to submit results.
  - We reuse as much of the existing Work Items / Jobs / 'record discovery' implementation as possible, to reduce implementation and maintenance effort.
* Security: Subscribers need to know it's actually us calling them and not a malicious actor.
  - We should use the industry standard for this which is **HMAC** (Hash-based Message Authentication Code).
  - At least **SHA256** should be used for the HMAC.
* Two subscriptions should be offered (to enable organisations to opt-in incrementally, and so that Custodian and Searcher contexts are kept seperate):
  1. `Job Created` subscription
  2. `Search Completed` subscription


## High-Level Design

* Subscription Management:
  * Org Directory will need optional ability for webhooks URLs to be supplied (eventually the Custodians will supply these to us).
  * Org Directory will need a private secret key per Custodian that we use to sign webhook payloads (HMAC), so that Custodians can verify to is us who has called them (payload is authentic), and that the payload hasn't been tampered with.
* Dispatching Notifications:
  * When a Job is created and the Custodian of the Job has a Job Created subscription,  
    Then the Custodian organisation should receive the notification on their Job Created webhook URL.
  * When a Search is completed and the initiating Searcher has a Search Completed subscription,  
    Then the Searching organisation should receive the notification on their Search Completed webhook URL.
  * Dispatching should use message queues, so that:
    * the notification functionality doesn't block the main functionality, and
    * we can keep track of webhooks that completely fail (to enable alerts to the organisation, or manual replays).
  * Processing of webhooks should include retries with backoff, up to a capped number of retries.
  * Auditing:
    * Should reuse the existing `AuditEvent`.
    * After calling a webhook URL, log an `AuditEvent` on success or failure.

* Maybe: "Ping" job
  * For organisations to be able to test polling/webhooks.
  * Question: how would this be invoked?  Admin endpoint / concept?
  * Also, possibly, some way of knowing which Custodians are listening and responding.
    - This would likely require someway of the Custodians replying to the ping and the SUI system storing that.