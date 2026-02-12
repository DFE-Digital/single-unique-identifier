# ADR-SUI-0015: Optional Webhook Notifications for Task Availability

Date: 05 February 2026  
Author: Simon Parsons  
Decision owners: SUI Service Team  
Category: Distributed discovery architecture

## Status
Draft — work in progress

---

## Overview

This ADR explores the potential introduction of webhook-based notifications as an
optional enhancement to the pull-based polling model used for distributed discovery
and lifecycle tasks.

The ADR makes clear that polling remains the canonical and mandatory integration
mechanism, and that webhooks, if introduced, act only as best-effort signals to
improve responsiveness.

This document does not yet propose a final design and will be developed alongside
decisions on custodian administration, authentication, and portal capabilities.
