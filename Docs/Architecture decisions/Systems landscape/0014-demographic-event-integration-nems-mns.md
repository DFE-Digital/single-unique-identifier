# ADR-SUI-0014: Demographic Event Integration for Identifier Lifecycle Triggers (NEMS / MNS)

Date: 2026-02-05
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Systems integration

## Status
Draft — work in progress

---

## Overview

This ADR captures the emerging approach for integrating the national discovery service
with NHS demographic event notification services, including NEMS and the Multicast
Notification Service (MNS).

Its purpose is to frame how authoritative demographic change events (for example NHS
number changes, record merges or splits) may trigger lifecycle actions for issued
identifiers, including refresh, invalidation, or reissuance tasks.

This document is intentionally incomplete and exists to reserve architectural space
for this decision and to capture context while discovery work continues.
