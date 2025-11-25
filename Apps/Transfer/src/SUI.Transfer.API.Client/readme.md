# SUI Transfer Client
This client is used for connecting to the SUI Transfer Service and is generated from the Open API Spec.
To use the client add the following lines into your program.cs with the correct base url and secret.

``` csharp
using SUI.Transfer.API.Client;
builder.Services.AddTransferClient("http://localhost:5062", "2f4a84d6d13a7fc2e08c06192c31646c89aefb4b5afd58694695d570");
```