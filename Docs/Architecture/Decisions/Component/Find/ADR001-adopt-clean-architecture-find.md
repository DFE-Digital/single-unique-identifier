# ADR002: Adopt Clean Architecture for Find

- **Date**: 2025-09-04
- **Author**: Stuart Maskell
- **Status**: Accepted
- **Category**: Component/Find

## Decision
 We will adopt the **Clean Architecture** pattern for our .NET 9+ API project(s) for the 'Find' team. This decision is based on its strong support for separation of concerns, testability, and its widespread adoption within the .NET community, which will ease developer onboarding. A good template to follow is [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture/tree/main)

## Context

For the 'Find' project, we are initiating a semi new project using .NET. The project will involve starting with one or two public-facing APIs deployed to Azure. The system will integrate with several external components:

* A database
* Secret storage
* File storage for logs
* Third-party APIs such as the NHS PDS FHIR API.

The key drivers for this architectural decision are the need for a structure that is easy for developers to adopt and test, while ensuring an excellent separation of concerns. This will support long-term maintainability and scalability as the project evolves.

## Options Considered

1.  **Diamond Architecture**: A more informal pattern compared to Clean Architecture where dependencies are structured to flow from outer layers (like UI and Infrastructure) towards an inner 'Application' layer, and finally to a central 'Core' or 'Domain' layer. It aims for simplicity and clear dependency flow.

2. (SELECTED) **Clean Architecture**: A well-established pattern that organizes the project into a series of concentric layers (e.g., Domain, Application, Infrastructure, Presentation). The fundamental rule is the **Dependency Rule**: source code dependencies can only point inwards. This isolates the core business logic from external concerns like databases, frameworks, and UIs.

3.  **Hexagonal Architecture (Ports and Adapters)**: Another pattern focused on isolating the core application logic. It interacts with the outside world through "Ports" (interfaces defined by the application) and "Adapters" (concrete implementations for external technologies like databases or APIs).

## Consequences

### Option 1. Diamond Architecture

* **Pros**:
    * Perceived Initial Simplicity: The visual dependency graph is intuitive and easy to explain on a whiteboard. For a team unfamiliar with formal architectural patterns, this can feel less intimidating than adopting the stricter, more prescriptive rules of Clean Architecture.
    * Potentially faster at the beginning of a project due to it's informal nature, developers can quickly develop without worrying to much about dependencies and projects. This is a trade-off for speed.
    * It's the closest pattern to Clean Architecture, so if the project needed to adhere to more strict dependencies, then it could move to a more formal Clean Architecture.
* **Cons**:
    * There is far less resource and documentation around this pattern, so going beyond simple applications may cause difficulties in understanding.
    * The lack of strict, community-enforced conventions might lead to inconsistencies and a breakdown in separation of concerns as the project grows.

### Option 2. Clean Architecture (Chosen)

* **Pros**:
    * **Excellent Separation of Concerns**: The strict dependency rule ensures that the core domain and application logic are completely independent of infrastructure details (e.g., the database or API framework).
    * **Highly Testable**: Business logic can be tested in isolation with minimal setup, as it has no direct dependencies on external systems. This perfectly aligns with our testing requirements.
    * **Maintainable & Flexible**: Swapping out external components (e.g., changing the database provider or a third-party service) is much easier, as changes are isolated to the Infrastructure layer.
    * **Widely Adopted**: There is a wealth of documentation, community support, and project templates (like Jason Taylor's) available for .NET, significantly reducing adoption friction.
* **Cons**:
    * Can introduce more upfront complexity and boilerplate (e.g., more projects in the solution, mapping between layers). However, this is a trade-off for long-term maintainability.

### Option 3. Hexagonal Architecture

* **Pros**:
    * Shares the core benefits of Clean Architecture regarding separation of concerns and testability.
    * The "Ports and Adapters" terminology can be very intuitive for developers when thinking about plugging in new integrations.
* **Cons**:
    * While the principles are well-known, concrete implementation structures and templates are slightly less standardized in the .NET community compared to Clean Architecture.
    * The conceptual difference between Hexagonal and Clean Architecture is often subtle, and the latter is more frequently cited and implemented in modern .NET projects.


## Advice

_Josh Taylor, Technical architect, 2025-09-04._ Go with clean architecture, mostly well known and easier for developers to onboard.

_Tom Hawkin, Senior developer, 2025-09-04._ Go with clean architecture, simplest and does follows SOLID principles well.