using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using SUI.Find.Application.Constants;
using SUI.Find.Infrastructure.Clients;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Factories;

public interface IQueueClientFactory
{
    IAuditQueueSender GetAuditClient();
}

[ExcludeFromCodeCoverage(Justification = "Simple factory class")]
public class QueueClientFactory(IConfiguration config) : IQueueClientFactory
{
    private readonly string _connectionString =
        config["AuditProcessorConnectionString"] ?? throw new InvalidOperationException();

    public IAuditQueueSender GetAuditClient()
    {
        return new AzureQueueSender(
            new QueueClient(_connectionString, ApplicationConstants.Audit.AccessQueueName)
        );
    }
}
