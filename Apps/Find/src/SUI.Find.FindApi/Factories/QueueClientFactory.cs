using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace SUI.Find.FindApi.Factories;

public interface IQueueClientFactory
{
    QueueClient GetAuditClient();
}

public class QueueClientFactory(IConfiguration config) : IQueueClientFactory
{
    private readonly string _connectionString =
        config["AzureWebJobsStorage"] ?? throw new InvalidOperationException();

    public QueueClient GetAuditClient() => new QueueClient(_connectionString, "audit-queue");
}
