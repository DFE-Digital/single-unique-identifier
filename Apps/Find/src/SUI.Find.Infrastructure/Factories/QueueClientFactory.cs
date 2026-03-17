using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using SUI.Find.Application.Constants;
using SUI.Find.Infrastructure.Clients;

namespace SUI.Find.Infrastructure.Factories;

public interface IQueueClientFactory
{
    IAuditQueueSender GetAuditClient();
    ISearchJobQueueSender GetSearchJobClient();
}

[ExcludeFromCodeCoverage(Justification = "Simple factory class")]
public class QueueClientFactory(IConfiguration config) : IQueueClientFactory
{
    private readonly string _auditConnectionString =
        config["AuditProcessorConnectionString"] ?? throw new InvalidOperationException();

    private readonly string _searchJobConnectionString =
        config["AzureWebJobsStorage"] ?? throw new InvalidOperationException();

    public IAuditQueueSender GetAuditClient()
    {
        return new AzureQueueSender(
            new QueueClient(_auditConnectionString, ApplicationConstants.Audit.AccessQueueName)
        );
    }

    public ISearchJobQueueSender GetSearchJobClient()
    {
        return new AzureSearchJobQueueSender(
            new QueueClient(_searchJobConnectionString, ApplicationConstants.SearchJobs.QueueName)
        );
    }
}
