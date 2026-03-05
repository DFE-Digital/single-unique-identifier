# Polling Discovery – User Stories Overview

This document lists the core user stories required to implement the Polling Discovery variant of the FIND architecture. Each story includes a brief summary of its purpose.

---

## 1. Implement Atomic Work Claim Endpoint

**Summary:**  
Implement `POST /work/claim` to atomically select and lease the next available work item for an authenticated custodian. This is the canonical polling mechanism and must be race-safe and storage-efficient.

---

## 2. Implement Work Result Submission Endpoint

**Summary:**  
Implement `POST /work/{workItemId}/result` to allow custodians to submit processing outcomes. The endpoint must validate lease ownership and expiry before accepting results.

---

## 3. Define Work Item Storage Model

**Summary:**  
Design and implement the persistent storage structure for work items, including status, lease owner, lease expiry, job linkage, and minimal required metadata.

---

## 4. Implement Lease Management Logic

**Summary:**  
Implement lease acquisition, validation, expiry handling, and (if required) renewal or resurrection behaviour to ensure correctness under retries and failures.

---

## 5. Implement Polling Controllers

**Summary:**  
Build the API controllers for claim and result endpoints, including authentication enforcement, trace propagation, backoff headers, and cache-control behaviour.

---

## 6. Implement Backpressure and Throttling Behaviour

**Summary:**  
Add support for `Retry-After`, 429 responses, and server-side throttling policies to protect infrastructure under load.

---

## 7. Refactor Searcher Result Retrieval

**Summary:**  
Refactor the existing search results endpoint so that results can be retrieved incrementally as custodians submit them, rather than only after fan-out completion.

---

## 8. Optional: Provide Advisory Availability Endpoint

**Summary:**  
Optionally implement `HEAD /work/available` as an advisory probe for work existence. This endpoint must not be relied upon for correctness.

---

## 9. Update Custodian Stub – Polling Behaviour

**Summary:**  
Update stub custodians to implement the polling loop: claim, backoff on 204, honour `Retry-After`, and handle throttling correctly.

---

## 10. Update Custodian Stub – Result Submission

**Summary:**  
Update stub custodians to perform simulated local lookup and submit structured results via the result endpoint.

---

## 11. Optional: Return Initial ID Register Results

**Summary:**  
Enhance the FindARecord endpoint to optionally include immediate results from the ID register before custodial responses are returned.

---

## 12. Implement Unattended Authentication (M2M)

**Summary:**  
Enable machine-to-machine authentication for custodians, including token validation, identity derivation, and role-based authorisation enforcement.
