# ADR-GetAnIdentifier-0002: Avoiding Duplicate MNS Subscriptions

Date: 2026-09-14

Author: Stuart Maskell

Category: System/GetAnIdentifier / NHS Integration

## Status

Accepted

Related to: [ADR-GetAnIdentifier-0001](0001-NHS-MNS-integration.md)

## Decision

Option 2 - Async task to remove duplicates AFTER subscribing. This gives us the fasted path to development whilst keeping dupicate handling.

## Context

We are going to be integrating with the NHS MNS service. We would prefer to avoid creating duplicate subscriptions as they are unnecessary. However, we have a strong directive to not store NHS numbers in our service. MNS also does not have its own duplicate avoidance system. The subscription service we will use to send subscriptions to MNS will be an asynchronous Task.

### Scale

We are expecting approximately **50,000 subscriptions** for the Alpha stage.

### MNS API constraints

These have been confirmed with NHS and are fixed constraints on any option we choose:

- MNS does **not** provide an endpoint or query parameter to search or filter subscriptions by NHS number. The only read operation available to us is retrieving the full list of our subscriptions.
- MNS subscription creation is **not** idempotent and does not upsert. An identical request sent twice creates two distinct subscriptions, so retries of a request that actually succeeded will themselves produce duplicates.

## Options considered

1. Get all subscriptions and traverse the list to look for an existing NHS number.
2. Async task to remove duplicates AFTER subscribing.
3. Hold subscription IDs against a keyed HMAC-SHA256 of the NHS number.
4. Do nothing.

## Consequences

### Option 1: Get all subscriptions and traverse the list

- **Positive:** Requires no additional infrastructure or database management as we can query all our subscriptions from the NHS MNS endpoint to look for existing subscriptions.
- **Negative:** Heavy and slow operation if the list is huge.
- **Negative:** Could have concurrency issues if two requests come in at near enough the same time and both see no existing subscriptions.
- **Negative:** Possibly a heavy operation for NHS depending on whether they cache or not.
- **Negative:** Could hit rate limits if we need to get the list on every request and often.

### Option 2: Async task to remove duplicates AFTER subscribing

- **Positive:** Subscribe to all incoming requests immediately and deal with duplicates later - no delay to sending subscriptions.
- **Positive:** Deals well with concurrency as it's a batch DELETE on a background task.
- **Negative:** As it's a batch delete, we would need to traverse the entire list of subscriptions and have logic to keep only 1 for each person.
- **Negative:** Could lead to duplicates in the short term, meaning possible double or more notifications for the same person. This does depend on how often we check for duplicates.

### Option 3: Keyed HMAC-SHA256 of the NHS number

- **Positive:** Local avoidance of duplication BEFORE any MNS subscriptions are created, with no dependency on MNS read operations.
- **Positive:** We are not storing NHS numbers in a recoverable form. The stored value is a pseudonymous identifier, not the NHS number itself.
- **Negative:** **The secret key is doing all of the protective work here.** There are only around 10^9 valid NHS numbers, so an unkeyed hash of an NHS number can be exhaustively enumerated cheaply. It is the secrecy of the HMAC key, not the hash function, that makes the stored values unusable to an attacker. Leakage of the key therefore compromises every value in the store, and the store would have to be rebuilt from scratch.
- **Negative:** This places hard requirements on key handling: the key must live in Key Vault / Managed HSM, be reachable only via managed identity, never appear in configuration, logs or source, and have a documented compromise response.
- **Negative:** The stored values remain pseudonymised personal data under UK GDPR. They are in scope for DPIA, retention and subject access obligations even though they are not NHS numbers.
- **Mixed (key rotation):** Existing rows cannot be re-keyed, because by design we no longer hold the NHS number. Rotation means tagging each row with a key version, checking incoming values against all active key versions, and retiring a version only once every row under it has aged out. This is workable and lets us rotate as often as needed, but the cost of rotation is a direct function of our retention period.

### Option 4: Do nothing

- **Positive:** Let duplicates happen, no additional logic or MNS calls for de-duplication.
- **Positive:** We could de-duplicate at the point of forwarding. If on the batch we see two of the same NHS numbers, we could just send one notification.
- **Negative:** We could end up with many notifications for the same person at the same time due to duplicates.
- **Negative:** Still makes more subscriptions than needed.

## Advice

Tech lead: Option 2 - fasted to deployment.
Senior Dev A: Option 2
Senior Dev B: Option 2
