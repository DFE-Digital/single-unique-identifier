# ADR-SUI-0016: API Edge Pattern for National Services (API Gateway / APIM)

Date: 05 February 2026  
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Platform architecture

## Status
Draft — work in progress

---

## Overview

This ADR considers how the national discovery APIs should be exposed and protected
at the platform edge.

It explores the role of an API gateway or management layer (such as Azure API
Management) in enforcing authentication, rate limiting, observability, versioning,
and developer experience across MATCH, FIND, and related services.

No decision is yet recorded. This document exists to scope the problem space and
identify the architectural implications of different edge patterns.
