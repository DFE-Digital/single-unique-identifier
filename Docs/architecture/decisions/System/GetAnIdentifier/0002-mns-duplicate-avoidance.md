# ADR-GetAnIdentifier-0002: Avoiding Duplicate MNS Subscriptions

Date: 2026-09-14

Author: Stuart Maskell

Category: System/GetAnIdentifier / NHS Integration

## Status

Accepted

Related to: [ADR-GetAnIdentifier-0001](0001-NHS-MNS-integration.md)

## Decision

Option 2 - Async task to remove duplicates AFTER subscribing. This gives us the fastest path to development whilst keeping duplicate handling.

## Context

We are going to be integrating with the NHS MNS service to create subscriptions for when changes of NHS numbers of a person occur.

In order to mitigate duplication, we need to consider whether to avoid duplication before subscription and/or after sending subscriptions.
The purpose of mitigating is to:

- Keep the NHS subscription no more than what we need. Avoid overburdening the NHS.
- Decrease the likelihood of SUI system from sending multiple notifications to the same agency for the same person.

We have a strong directive to not store NHS numbers in our service, so this eliminates non encrypted forms of NHS number in any persistent store.

Except for Option 4 - We can collapse duplicates at the retrieval stage of messages by checking for duplicate NHS numbers.
This is a good preventative measure to avoid duplication before sending notifications to agencies. However, this solves only avoiding duplicate notifications.

### Scale

We are expecting to create approximately **50,000 subscriptions** for the Alpha stage.

### MNS API constraints

These have been confirmed with NHS and are fixed constraints on any option we choose:

- MNS does **not** provide an endpoint or query parameter to search or filter subscriptions by NHS number. The only read operation available to us is retrieving the full list of our subscriptions.
- MNS subscription creation is **not** idempotent and does not upsert. An identical request sent twice creates two distinct subscriptions, so retries of a request that actually succeeded will themselves produce duplicates.

## Options considered

1. On each NHS number request, get all subscriptions and check if the NHS number is already subscribed
2. Create a subscription for each NHS number requested, then have a regular background task to clean up duplicates
3. Hold subscription IDs against a keyed HMAC-SHA256 of the NHS number.
4. Do nothing.

## Consequences

### Option 1: Get all subscriptions and traverse the list to look for an existing NHS number

- **Positive:** Requires no additional infrastructure or database management as we can query all our subscriptions from the NHS MNS endpoint to look for existing subscriptions.
- **Negative:** Could have concurrency issues if two requests come in at near enough the same time and both see no existing subscriptions.
- **Negative:** Possibly a heavy operation for NHS depending on whether they cache or not.
- **Negative:** Could hit rate limits if we need to get the list on every request.

### Option 2: Do nothing before subscribing, then have a async task to remove duplicates AFTER subscribing

- **Positive:** Subscribe to all incoming requests immediately and deal with duplicates later - no delay to sending subscriptions.
- **Positive:** Deals well with concurrency as it's a batch DELETE on a background task.
- **Negative:** As it's a batch delete, we would need to traverse the entire list of subscriptions and have logic to keep only 1 for each person.
- **Negative:** Could lead to duplicates in the short term, meaning possible double or more notifications for the same person. This does depend on how often we check for duplicates. Clients will need to handle duplicate notifications until the clean-up task has removed the duplicate subscriptions.

### Option 3: Keyed HMAC-SHA256 of the NHS number

- **Positive:** Local avoidance of duplication BEFORE any MNS subscriptions are created, with no dependency on MNS read operations.
- **Positive:** We are not storing NHS numbers in a recoverable form. The stored value is a pseudonymous identifier, not the NHS number itself.
- **Negative:** **The secret key is doing all of the protective work here.** There are only around 10^9 valid NHS numbers, so an unkeyed hash of an NHS number can be exhaustively enumerated cheaply. It is the secrecy of the HMAC key, not the hash function, that makes the stored values unusable to an attacker. Leakage of the key therefore compromises every value in the store, and the store would have to be rebuilt from scratch.
- **Negative:** This places hard requirements on key handling: the key must live in Key Vault / Managed HSM, be reachable only via managed identity, never appear in configuration, logs or source, and have a documented compromise response.
- **Negative:** The stored values remain pseudonymised personal data under UK GDPR. They are in scope for DPIA, retention and subject access obligations even though they are not NHS numbers.
- **Mixed (key rotation):** Existing rows cannot be re-keyed, because by design we no longer hold the NHS number. Rotation means tagging each row with a key version, checking incoming values against all active key versions, and retiring a version only once every row under it has aged out. This is workable and lets us rotate as often as needed, but the cost of rotation is a direct function of our retention period.

### Option 4: Do nothing

- **Positive:** Let duplicates happen, no additional logic or MNS calls for de-duplication.
- **Negative:** We could end up with many notifications for the same person at the same time due to duplicates.
- **Negative:** Makes more subscriptions than needed and list would grow over time without any limit.

## Advice

Tech lead: Option 2 - fastest to deploy.
Senior Dev A: Option 2
Senior Dev B: Option 2
