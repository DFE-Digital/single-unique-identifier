# ADR001: Mocking Tool

- **Date**: 2025-09-04
- **Author**: Tom Hawkin
- **Status**: Draft
- **Category**: Component/Common

## Decision

Option 1: Wiremock.Net
We have decided to use WireMock.Net as our primary tool for mocking external API dependencies for our C# projects, as it can be run both as a standalone server and embedded directly within our test suites.

## Context

To enable parallel development and robust testing, our teams need to simulate the behaviour of external HTTP-based APIs. We rely on several third-party services that may be unavailable during development, have rate limits, or are difficult to configure for specific test scenarios (e.g., error states, network latency).

Adopting a standardised mocking tool will allow components to be built and tested independently of these external services. It will also enable the creation of reliable and repeatable automated tests that can simulate a wide range of API responses, including edge cases and failures, without making actual network calls. This decision aims to select a single tool to ensure consistency across all development and testing workflows.

## Options considered

1. (Selected) Wiremock.Net
2. MockServer
3. Mockoon

## Consequences

### Option 1: Wiremock.Net

WireMock.Net is the .NET port of the popular open-source mocking tool for HTTP-based APIs, providing full compatibility and integration for C# projects.

- Reduced Test Maintenance: Because WireMock.Net can run both standalone and embedded, we can reuse the same mock definitions across different test stages. This ensures our mocks behave consistently everywhere and significantly reduces the effort required to maintain separate mock setups.

- Increased Team Collaboration: Its support for both fluent C# and simple JSON configuration means developers and QA engineers can use the format they are most comfortable with. This lowers the barrier to entry and prevents the task of creating mocks from becoming a bottleneck dependent on a single role.

- Higher Confidence in Releases: The ability to simulate stateful behavior allows us to accurately test complex, multi-step user workflows. This provides higher confidence that our system can handle real-world scenarios before deployment.

- Faster Initial Development: The record-and-playback feature drastically reduces the time needed to create mock setups for existing third-party APIs. This allows development teams to start building dependent components much sooner, accelerating project timelines.

- Risk of Unmanaged Complexity: For services with many endpoints, the number of JSON stub files can become large. Without a clear file organization and naming strategy, this could slow down development as engineers struggle to find and modify the correct mock, increasing the risk of configuration errors.

### Option 2: MockServer

MockServer is another powerful tool for mocking and proxying HTTP/S services.

- Easier Debugging of Tests: Its built-in dashboard UI provides a clear view of incoming requests and active mocks. This can significantly speed up the process of diagnosing failing tests by making it easy to see exactly what request our system sent.

- Increased Test Complexity and Slower Feedback: As it can only run as an external process for .NET projects, our test suites would become more complex, needing to manage the lifecycle of a separate server or Docker container. This adds overhead to each test run, slowing down the local development feedback loop and making tests more brittle.

- Proxying and Recording: Similar to WireMock.Net, it can record and playback traffic from live services.

- External Process Only: For a .NET project, MockServer must always run as an external process (e.g., in Docker). It cannot be embedded directly into the test process, which complicates test setup, teardown, and debugging compared to WireMock.Net.

- Higher Maintenance Overhead: The lack of an official .NET client means we would depend on a community-maintained library or have to write and maintain our own client. This introduces a long-term maintenance burden and a supply chain risk if the community project becomes inactive.

### Option 3: Mockoon

Mockoon is a free, open-source tool that allows you to quickly design and run mock APIs. It's known for its intuitive desktop GUI.

- Empowers Non-Technical Roles. Its intuitive GUI allows non-developers to create and run mocks independently. This would unblock parallel workstreams, as other teams wouldn't need to wait for a backend developer to provide a mock endpoint.

- Limited Automated Test Assertions. Because it cannot be programmatically controlled from our C# test code, it's difficult to perform fine-grained verifications (e.g., "assert this endpoint was called exactly once"). This would limit the depth of our automated integration tests, potentially allowing bugs in our system's outbound API calls to go unnoticed.

- Inability to Test Complex Scenarios. Simulating stateful user flows is more difficult than in code-first tools. This would prevent us from adequately testing critical, multi-step business processes, reducing our confidence in the system's robustness for complex user journeys.

## Advice

Stuart Maskell - Wiremock.Net. It's .NET integrated and as we are a .NET team, it. fits nicely in local development and it should be easy to onboard and understand. We also use it in our current Repo so some of us are already familiar. Works well in CI/CD too so for me it's the winner.
