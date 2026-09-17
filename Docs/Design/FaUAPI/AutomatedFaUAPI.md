# Automation Capabilities of Find and Use API

**Date:** `2026-08-03`  
**Owner:** SUI Service Team  
**Scope:** A development plan which talks about how we are able to automate the FaUAPI so we can reduce the amount of manual steps and therefore human error that we encounter.

---

## 1. Purpose

This document discusses the implementation details of automating our endpoints using the Find and Use API (FaUAPI)

## 2. Scope and Non-Goals

### 2.1 In scope

- Manually setting up the API in FaUAPI
- Publishing SUI APIs to the FaUAPI platform using the upcoming export automation feature
- Hosting SUI API logic using Azure Functions.
- Managing API schema updates and versioning.
- Environment Plan

### 2.2 Out of scope

- FaUAPI API Groups
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
- Subscriptions are managed through our own workspace
- Once subscribed, users authenticate requests using their assigned subscription keys and OAuth 2 using FaUAPI.
- FaUAPI manages privacy levels on a Workspace/API/API Group level
- FaUAPI manages environments

## 6. Automation in the FaUAPI Dashboard

There is a soon to be released feature in `Workspace > API > Administration > Export Definitions` which will output a zip folder with all the necessery files and permissions for us to setup an automation pipeline in github.

### 6.1 Manual Setup

Manually setting up the API initially is the easiest way to get the automation started.

We will utilise the FaUAPI Add API feature `Workspace > Add` to tell FaUAPI about our hosted endpoints. Once we have added the relevant details we will need to go into the API to give it our OpenAPI Schema and configure it to be published.

### 6.1.1 Publishing

In order for our changes to take effect we need to publish the changes. The publish flow `Workspace > API > Configuration > Publish` is really helpful in that it will tell you if you have missed some setup, and if you are publishing after changing the API it will tell you exactly what has changed.

There is currently no ability to go back to a previously published configuration, so care must be taken when doing this manually.

### 6.2 Environments

There is a beta feature at the moment which allows us to create multiple environments in a single workspace `Workspace > API > Configuration > Environments`. We need to request access to this feature using the service desk and it will be available on our workspace.

Previously this document detailed using different workspaces as environments, however since this feature is available to us, it makes sense to utilise it.

### 6.3 Authentication

`Workspace > API > Configuration > Authentication` Allows us to set up how we want users to authenticate with FaUAPI. We will use this section to setup the OAuth flow which will be used by our subscriptions to connect to our API.

FaUAPI will manage the OAuth flow and forward the authentication header on to us to verify the user is allowed to access our backend.

### 6.4 Policies

We can go to `Workspace > API > Configuration > Policies` to set standard APIM XML policies on our API. These can be done at the API / Environment / Operation scopes.

Each scope should inherit from the level above using the `</base>` tag (with the exception of the API scope which is the top level)

### 6.5 Automated pipelines

This feature is currently not released, however we have requested the readme documentation in order to update this document and write tickets ready for when it is released. But in essence this will give us everything we need to setup pipelines that will detect changes and publish those changes to FaUAPI. The initial version will not include rollbacks however this is on their roadmap.

## 7. Automation in Azure

We are able to manage our own IaC for all the logic of our application since FaUAPI is largely a gateway and catalogue

### 7.1 Azure Functions

Our existing functions are hosted in Azure functions and you provide a url that FaUAPI can redirect calls to.

This will need to be setup as a private link and the subscription name sent to the FaUAPI team via the service desk. They will then be able to set it up on their end so it works.

### 7.3 OAuth2

We need to configure environment variables in the function app to point to FaUAPI OAuth issuer. We don't need to deploy our auth emulator.

## 8. Privacy

### 8.1 Workspace Privacy

Workspaces in FaUAPI enforce a default privacy level.

### 8.2 Granular Visibility

Visibility can be restricted at the individual API level or via API groups.

### 8.3 Private Keys

Private keys can be configured in our IaC. The FaUAPI team just need our subscription name to set it up on their end.

## 9. Subscription Keys

- FaUAPI issues subscription keys upon approval of a subscription request.
- Calls to user-restricted endpoints require OAuth 2.0 tokens and pre-registered redirect URLs.
- When the MAIS Dashboard is built we will be able to utilise the `APISubscriptionTask` API to manage the subscription requests in the MAIS Dashboard instead of the FaUAPI Dashboard.
- We approve and manage the subscription request ourselves.
- Subscriptions include all auth details that a user needs to use to access our endpoint.

## 10. Environment management

FaUAPI offers 2 seperate environments

- Pre-Prod: <https://pp-apimanagement.education.gov.uk/>
- Prod: <https://apimanagement.education.gov.uk/>

There is a beta feature that will let us create environments inside of the FaUAPI and we can create individual subscriptions and policies to each of them.
