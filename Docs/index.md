# What is 'single unique identifier'?

Approved public integration and onboarding guidance will be published through the [documentation site](../public-docs/README.md). This site is being established; existing material remains in this repository until it has been reviewed for publication.

This project investigates the technical foundations required to help practitioners improve safeguarding and welfare of children by accessing the right information at the right time, while maintaining strong standards of privacy, security, and data minimisation.

The programme is working towards **Alpha**, with production-grade expectations for code, infrastructure, testing, security, operability, observability and documentation. Explicitly experimental work should be labelled as such.

## Current service scope

Get an Identifier is the main vendor-facing API: demographics are searched against NHS England's PDS to return the matched NHS number and GP practice ODS code where available. The Alpha direction extends this with MNS subscriptions, lifecycle notifications received through MESH, and a separate Notification Service that notifies registered supplier webhook endpoints. Suppliers rematch through Get an Identifier; webhooks do not carry replacement identity or demographic information.

PDS matching is implemented. MNS subscription creation and end-to-end lifecycle processing are not implemented on `main`. The Notification Service retrieves and parses MESH messages. Its webhook register and signed delivery components are not invoked by the orchestrator, and messages remain unacknowledged. The supplier lifecycle contract remains Draft.

Use app READMEs and as-built documentation for implemented behaviour, accepted ADRs for decisions, and draft contracts for proposals. Older `MATCH`, `FIND`, `FETCH`, distributed discovery, polling and jobs material is retained for context, not as the current primary Alpha scope.

## Contents

### Integration

- [System integration documentation pack](./Integration/Index.md)

### Architecture

- [Architecture models and their scope](./architecture/models/README.md)
- [Architecture decisions and current direction](./architecture/decisions/index.md)

### Design

- [Get an Identifier as-built design](./Design/GetAnIdentifier/AsBuilt.md)
- [Notification Service implementation and deployment](../Apps/NotificationService/README.md)
- [Supplier lifecycle webhook contract v1 (draft)](./Design/Notifications-Webhooks/SupplierLifecycle/V1/Index.md)
- [Polling API Design (earlier proposed distributed discovery design)](./Design/Variants/PollingDiscovery/Polling/Endpoints.md)
- [Authentication and API Edge Strategy (superseded planning reference)](./Design/Authentication/Index.md)
- [Authentication Baseline and Security Model (superseded planning reference)](./Design/Authentication/BaselineSecurityModel.md)
- [Authentication Environment Strategy (superseded planning reference)](./Design/Authentication/EnvironmentStrategy.md)

### Development

- [CI workflows](./Developers/ci-workflows.md)
- [Self-hosted runner and artifact storage](./Developers/ci-self-hosted-runner.md)
