# Automation Capabilities of Find and Use API

**Date:** `2026-08-03`  
**Owner:** SUI Service Team  
**Scope:** A development plan which talks about how we are able to automate the FaUAPI so we can reduce the amount of manual steps and therefore human error that we encounter.

---

## 1. Purpose

This document discusses the implementation details of automating our endpoints using the Find and Use API (FaUAPI)

## 2. Scope and Non-Goals

### 2.1 In scope

- Publishing SUI APIs to the FaUAPI platform using Import API.
- Hosting SUI API logic using Azure Functions.
- Managing API schema updates and versioning.
- Environment Plan

### 2.2 Out of scope

- FaUAPI API Groups
- FaUAPI Add API
- Exposing these specific APIs to external or public consumers.
- Bypassing the standard FaUAPI subscription and access models.
- Setting up all environments

## 3. Principles

The implementation plan is built around the following principles:

### 3.1 Automated Infrastructure

Automation speeds up development time and reduces the risk of human error when configuring infrastructure

### 3.2 Privacy

Due to the nature of the what the APIs are used for and FaUAPI being a catalogue of DfE APIs we want to ensure that these endpoints are only discoverable by the right people.

## 4. Model

### 4.1 Gateway Layer

FaUAPI (Azure API Management) handles routing and basic gateway policies.

### 4.2 Backend Hosting

Azure Functions host the executable API code.

## 5. Context

- We must upload an API schema to FaUAPI that points to our backend hosted in Azure Functions
- Access to specific APIs requires an approved subscription that is managed in the FaUAPI Dashboard.
- Subscriptions take up to 3 days to approve
- Once subscribed, users authenticate requests using their assigned subscription keys and OAuth 2.
- FaUAPI manages privacy levels on a Workspace/API/API Group level

## 6. Automation in FaUAPI

We are limited in what we can automate through FaUAPI to what is exposed to us. However we can achieve some level of automation.

### 6.1 Import API

We will utilise the FaUAPI schema import feature, pointing to a secure HTTPS URL hosting our OpenAPI specification. We will utilise the daily sync option to ensure changes to our endpoints are managed automatically

### 6.2 Manual Updating

Import API also has the option for manual resync which we may choose to utilise if daily sync is too infrequent for our service

## 7. Automation in Azure

We are able to manage our own IaC for all the logic of our application since FaUAPI is largely a gateway and catalogue

### 7.1 Azure Functions

Our existing functions are hosted in Azure functions and you provide a url that FaUAPI can redirect calls to.

### 7.2 Schema Hosting

Import API takes in a single schema which is used to generate all the APIs which is more efficient for updating than the Add API. FaUAPI can get access to our schema files that are hosted in the Azure Function App. To achieve this we need to get the default domain and append `/api/swagger.json`.

### 7.3 OAuth2

There should be no change to current implementation

## 8. Privacy

### 8.1 Workspace Privacy

Workspaces in FaUAPI enforce a default privacy level.

### 8.2 Granular Visibility

Visibility can be restricted at the individual API level or via API groups.

## 9. Subscription Keys

- FaUAPI issues subscription keys upon approval of a subscription request.
- Calls to user-restricted endpoints require OAuth 2.0 tokens and pre-registered redirect URLs.

## 10. Environment management

FaUAPI offers 2 seperate environments

- Pre-Prod: <https://pp-apimanagement.education.gov.uk/>
- Prod: <https://apimanagement.education.gov.uk/>

I propose that our environments are made using different workspaces with everything upto and including Pre-Prod existing in the pre-prod version of FaUAPI and having a workspace in then the prod environment only exists in prod version of FaUAPI.

FaUAPI also has sandbox workspaces which will get cleared down if it is not active, I propose that we only use sandbox workspaces as a way of testing experimental changes in isolation.

## 11. Open Points Carried Forward

### 11.1 Private Connectivity

- Do we require Azure Private Endpoints if workspace and API-level visibility are tightly restricted?
- Confirm whether FaUAPI supports routing through Private Links for Azure Functions before including in implementation.

### 11.2 Automated Subscriptions

We need to consult the FaUAPI platform team regarding an API or programmatic method to expedite the 3-day subscription approval process for internal consumers.

### 11.3 Rate Limiting, Threat Protection, and Backend Security Patterns

Consult the FaUAPI team regarding their recommended pattern for rate limiting, WAF/threat protection, and backend protection prior to introducing Azure Front-Door into the design.

## 12. Immediate Implementation Implications

- Configure the FaUAPI workspace with the correct privacy defaults and register the API.
- Configure the API using Import API instead of Add API.
