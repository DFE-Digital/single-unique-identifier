# SUI Custodians Client

This client is used for connecting to the SUI Custodians Service and is generated from the Open API Spec.
To use the client add the following lines into your program.cs with the correct base url and secret.

``` csharp
using SUI.Custodians.API.Client;
builder.Services.AddCustodiansClient("http://localhost:<port>", "<your-api-key>");
```