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

## 6. Automation in the FaUAPI Dashboard

We are limited in what we can automate through FaUAPI Dashboard. There is a FaUAPI [API](https://apimanagement.education.gov.uk/api/schema/index.html) that can be triggered in pipelines to limit manual configuration. However there are things we have open questions on with the FaUAPI development team so this section is still relevant.

### 6.1 Import API

We will utilise the FaUAPI schema import feature, pointing to a secure HTTPS URL hosting our OpenAPI specification. We will utilise the daily sync option to ensure changes to our endpoints are managed automatically. Currently we have an open [ticket](https://hippodigital-dfe.atlassian.net/browse/SUI-1962) to expose the `/api/swagger.json` endpoint.

Manual Updating - Import API also has the option for manual resync which we may choose to utilise if daily sync is too infrequent for our service

## 7. Automation in Azure

We are able to manage our own IaC for all the logic of our application since FaUAPI is largely a gateway and catalogue

### 7.1 Azure Functions

Our existing functions are hosted in Azure functions and you provide a url that FaUAPI can redirect calls to.

### 7.2 Schema Hosting

Import API takes in a single schema which is used to generate all the APIs which is more efficient for updating than the Add API. FaUAPI can get access to our schema files that are hosted in the Azure Function App. To achieve this we need to get the default domain and append `/api/swagger.json`.

### 7.3 OAuth2

There should be no change to current implementation but this will be confirmed during the FaUAPI spike.

## 8. Automation in Pipelines

### 8.1 Policies

Use the `APIPolicyTask` or `APIOperationPolicyTask` to create or update the policies for the endpoint rather than manually editing an XML document in the dashboard.

Policies are where we will set rate limits etc...

Verify it is working by looking in the dashboard

### 8.2 Documentation

Use the `APIDocumentTask` to create or update the the documentation subscribers see inside the FaUAPI dashboard

Verify it is working by looking in the dashboard

### 8.3 Release Notes

Use the `APIReleaseTask` to publish our release notes for new versions.

Verify it is working by looking in the dashboard

## 9. Privacy

### 9.1 Workspace Privacy

Workspaces in FaUAPI enforce a default privacy level.

### 9.2 Granular Visibility

Visibility can be restricted at the individual API level or via API groups.

### 9.3 Private Keys

Private keys can be configured in our IaC however we need to understand what the FaUAPI team need for them to create it on their end.

## 10. Subscription Keys

- FaUAPI issues subscription keys upon approval of a subscription request.
- Calls to user-restricted endpoints require OAuth 2.0 tokens and pre-registered redirect URLs.
- When the MAIS Dashboard is built we will be to utilise the `APISubscriptionTask` API to manage the subscription requests in the MAIS Dashboard instead of the FaUAPI Dashboard

## 11. Environment management

FaUAPI offers 2 seperate environments

- Pre-Prod: <https://pp-apimanagement.education.gov.uk/>
- Prod: <https://apimanagement.education.gov.uk/>

I propose that our environments are made using different workspaces with everything upto and including Pre-Prod existing in the pre-prod version of FaUAPI and having a workspace in then the prod environment only exists in prod version of FaUAPI.

FaUAPI also has sandbox workspaces which will get cleared down if it is not active, I propose that we only use sandbox workspaces as a way of testing experimental changes in isolation.

## 12. Open Points Carried Forward

### 12.1 Import API

Is it easier to use the FaUAPI API over the dashboard? The FaUAPI team will give us some examples of how to use the Import API as it provides a lot of configuration options, we may favour this over the dashboard.

## 13. Immediate Implementation Implications

- Configure the FaUAPI workspace with the correct privacy defaults and register the API.
- Configure the API using Import API instead of Add API.
