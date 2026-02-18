using Azure.Data.Tables;

namespace SUI.Find.Infrastructure.IntegrationTests;

public static class TableStorageFixture
{
    private static readonly Lazy<TableServiceClient> LazyClient = new(() =>
        new TableServiceClient("UseDevelopmentStorage=true")
    );

    public static TableServiceClient Client => LazyClient.Value;
}
