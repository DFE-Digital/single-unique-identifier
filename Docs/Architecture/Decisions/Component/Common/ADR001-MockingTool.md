# ADR001: Mocking Tool

- **Date**: 2025-09-04
- **Author**: Tom Hawkin
- **Status**: Draft
- **Category**: Component/Common

## Decision

TBC

## Context

To enable parallel development and robust testing, our teams need to simulate the behaviour of external HTTP-based APIs. We rely on several third-party services that may be unavailable during development, have rate limits, or are difficult to configure for specific test scenarios (e.g., error states, network latency).

Adopting a standardised mocking tool will allow developers to build and test components independently of these external services. It will also empower our quality assurance (QA) team to create reliable and repeatable automated tests that can simulate a wide range of API responses, including edge cases and failures, without making actual network calls. This decision aims to select a single tool to ensure consistency across all development and testing workflows.

## Options considered

1. Wiremock
2. MockServer
3. Mockoon

## Consequences

### Option 1: Wiremock

- WireMock is a popular, open-source mocking tool for HTTP-based APIs. It can be run as a standalone process or embedded within a c# application.

- Simple Configuration: Stubs are defined using human-readable JSON files, making them easy to create, read, and manage, even for non-developers.

- Standalone and Embeddable: It can be run as a standalone server, which is ideal for shared testing environments (CI/CD), or embedded directly into our test suites for more isolated, self-contained tests.

- Stateful Behaviour Simulation: WireMock can simulate stateful scenarios (e.g., an API that returns different responses based on previous requests), which is crucial for testing complex user flows.

- Request Matching and Verification: Offers powerful request matching on URL, headers, and body content. It also allows verification that a specific request was or was not received, which is invaluable for testing.

- Record and Playback: Can act as a proxy, recording traffic to a real API and converting it into stubs. This significantly speeds up the initial creation of mocks.

- JSON File Management: For very complex APIs with hundreds of endpoints and scenarios, managing a large number of individual JSON files can become cumbersome without a clear organisational strategy.

### Option 2: MockServer

MockServer is another powerful tool for mocking and proxying HTTP/S services.

- Flexible Deployment: Can be run as a standalone process or a Docker container, making it easy to integrate into a CI/CD pipeline.

- Built-in UI: Includes a dashboard UI for viewing active expectations and analysing received requests, which can aid in debugging.

- Proxying and Recording: Similar to WireMock, it can record and playback traffic from live services.

- External Process Only: For a .NET project, MockServer must always run as an external process (e.g., in Docker). It cannot be embedded directly into the test process, which complicates test setup, teardown, and debugging compared to WireMock.Net.

- No Official .NET Client: There is no official client library for .NET. Teams must rely on community-maintained clients or interact with MockServer's REST API directly, adding potential risk and maintenance overhead.

### Option 3: Mockoon

Mockoon is a free, open-source tool that allows you to quickly design and run mock APIs. It's known for its intuitive desktop GUI.

- Excellent User Interface: Its main feature is a clean, modern GUI that makes creating and managing mock endpoints extremely easy and accessible to everyone, including non-developers.

- No Coding Required: Mocks can be fully configured through the UI, lowering the barrier to entry for team members who are not familiar with API mocking.

- CLI for Automation: Provides a CLI tool that allows mock environments defined in the GUI to be run in headless environments like a CI/CD pipeline.

- Dynamic Templating: Supports dynamic and realistic data in responses using various templating helpers.

- Limited Programmatic Control: It is not designed to be embedded and controlled from within a test suite (e.g., starting a mock, asserting a request was made, and stopping the mock all within one C# test). This makes it less suitable for fine-grained, automated integration tests.

- State Management Complexity: While possible, creating complex stateful scenarios is generally more difficult to configure and manage than with WireMock's dedicated scenario-based features.

## Advice
