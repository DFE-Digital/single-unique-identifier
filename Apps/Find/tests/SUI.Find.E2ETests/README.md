# E2E testing

## What this is for

E2E tests are designed to be as light as possible and to check as little as possible. Leave fine details to Unit/Integration testing.
In this project we are testing the Happy path to ensure all components are functioning correctly.

## How to run it locally

Because azure dotnet isolated function do not play well in memory, this 
requires you to test against running applications.

1. Ideally, clean out your Azurite storage tables for a clean run.
2. Start the SUI.Find.FindApi and SUI.Find.CustodianSimulator.
   - Jetbrains Rider can throw up runtime issues with durable functions so it's recommended to use the function app CLI 'func'
3. Run the E2E tests in CLI or an IDE of your choice.
4. Stop the SUI.Find.FindApi and SUI.Find.CustodianSimulator. 