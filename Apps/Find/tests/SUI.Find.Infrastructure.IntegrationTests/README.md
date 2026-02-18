# SUI.Find.Infrastructure.IntegrationTests

This project contains low-level Integration Tests that validate interactions with external dependencies like Azure Storage.

To run these tests, first run Azurite:

```
docker run -d -p 10000:10000 -p 10001:10001 -p 10002:10002 --name sui-azurite mcr.microsoft.com/azure-storage/azurite
```
