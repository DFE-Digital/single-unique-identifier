# ADR001: Use ReqNRoll and Playwright to write and implement system tests

- **Date** : 2025-08-27
- **Author** : Stuart Maskell
- **Status** : Accepted
- **Category** : Component/SystemTests

# Decision

Option 1 \- ReqNRoll \+ Playwright.

# Context

We are working with a .NET 9 codebase. The value of the resulting system depends on multiple components working and integrating correctly. These components are being developed by different teams, so a shared understanding of system behavior is essential.

We are considering a BDD-style testing framework for full system tests. Such tests must provide high confidence in end-to-end behavior, align with user requirements, and illustrate intended usage. The idea arose to use BDD tests as a “source of truth” for correct system behavior. This would allow us to communicate scenarios clearly to stakeholders, and also drive work items for teams to progressively implement functionality.

We expect to encounter many system-wide edge cases, which makes strong system testing critical. Human-readable specifications are therefore important. Gherkin syntax (Given, When, Then) is under consideration to support this clear, shared understanding between developers, testers, and stakeholders.

# Options considered

1. ReqNRoll \- SpecFlow successor. https://reqnroll.net/  
2. LightBDD \- [https://github.com/LightBDD/LightBDD](https://github.com/LightBDD/LightBDD)  
3. Playwright-BDD \- [https://playwright.dev/](https://playwright.dev/) and https://vitalets.github.io/playwright-bdd/\#/  
4. Do nothing

# Consequences

## Option 1: ReqNRoll \+ Playwright

* Gherkin `.feature` files. Good because they express system behavior in plain language that both technical and non-technical stakeholders can understand.  
* Excellent IDE support for Visual Studio and Rider and Visual Studio Code.  
* Actively maintained with .NET 9+ support so we know that updates to issues are likely to be addressed in a timely manner, also that it will update with new Dotnet versions. Additionally they have paid support which gives confidence that this technology is well supported.  
* Requires developers to learn Gherkin syntax which though not complex, is an additional concept to learn.  
* Works with all the major Dotnet test runners. E.g. XUnit

## Option 2: LightBDD

* Fluent C\# API, which allows easy to write and refactor tests for Dotnet developers.  
* No Gherkin .feature files which will be harder for non-technical stakeholders to engage if they are viewing the tests. This however is mitigated slightly by using naming conventions. Simple example: 

```c#
Given_User_Views_Education_Page(“UserName”).And_Person_Record_Has_NHS_Number(“Person”).Then_User_Can_See_Latest_Attendance_Record(“UserName”)
```

* Maintenance updates are infrequent. Infrequent updates mean the framework is less likely to introduce breaking changes and can be relied on for consistent behavior over time, however, this may limit timely fixes for bugs or compatibility with newer .NET versions.  
* Works with all the major Dotnet test runners. E.g. XUnit

## Option 3: Playwright BDD JS

* Gherkin `.feature` files. Good because they express system behavior in plain language that both technical and non-technical stakeholders can understand.  
* Actively maintained with .NET 9+ support so we know that updates to issues are likely to be addressed in a timely manner, also that it will update with new Dotnet versions.  
* Though primarily used for web browser testing, it does support API, database testing, so we can test system changes as well.  
* Requires developers to know Typescript, and the NPM ecosystem. [Node.Js](http://Node.Js) dependency, this would likely be the biggest bottleneck and time consuming consequence.  
* Requires developers to learn Gherkin syntax which though not complex, is an additional concept to learn.  
* Having multiple languages in the repo increases overhead, complexity and cognitive load.

## Option 4: Do nothing

* No additional setup and framework testing complexity.  
* Low confidence in full system integration which would result in a high chance of regression bugs and missing requirements.  
* Lose the opportunities to understand requirements from writing them out and testing on them.

# Advice

*Josh Taylor \- ReqNRoll preferred choice \- Gherkin feature easily readable. Good support and actively maintained. Option 3 should be ruled out as it’s not Dotnet which DfE favours.*

*Daniel Murray \- No strong opinion.*

