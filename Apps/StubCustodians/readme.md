# StubCustodians

This C# .NET solution contains the Custodians API spec, and a stub implementation

## Generating and Publishing a new version of the Client SDK

The command here are expected to be run from `cd ./Apps/StubCustodians` relative to the repo root.

To generate a new version of the `SUI.Custodians.API.Client` from the code, run:

```
dotnet pwsh ./GenerateClient.ps1
```

Once you are happy with the generated code, a new version of the package can be published by running:

```
dotnet pwsh ./PushClient.ps1 -PackageVersion 1.0.0 -Configuration Release
```

Replacing `1.0.0` with the desired new version number.

You will be asked for a GitHub Personal Access Token (PAT), which you can generate from GitHub:

1. Click on your photo in the top right > click Settings > Developer Settings
2. Click Personal access tokens > Tokens (classic)
3. Click Generate new token > Generate new token (classic)
4. Add a note to describe the token > set an expiration date > check the `write:packages` permission
5. Click Generate token. Save a copy of the token on your machine incase you need it in the future.
