using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using SUI.Find.Application.Constants;
using SUI.Find.Infrastructure.Clients;

namespace SUI.Find.Infrastructure.Factories;

public interface IQueueClientFactory
{
    IAuditQueueSender GetAuditClient();

    IJobResultsQueueSender GetJobResultsClient();
}

[ExcludeFromCodeCoverage(Justification = "Simple factory class")]
public class QueueClientFactory(IConfiguration config) : IQueueClientFactory
{
    private readonly string _auditConnectionString =
        config["AuditProcessorConnectionString"] ?? throw new InvalidOperationException();

    private readonly string _jobResultsConnectionString =
        config["JobResultsConnectionString"] ?? throw new InvalidOperationException();

    public IAuditQueueSender GetAuditClient()
    {
        return new AzureQueueSender(
            new QueueClient(_auditConnectionString, ApplicationConstants.Audit.AccessQueueName)
        );
    }

    public IJobResultsQueueSender GetJobResultsClient()
    {
        return new AzureQueueSender(
            new QueueClient(
                _jobResultsConnectionString,
                ApplicationConstants.Queues.JobResultsQueueName
            )
        );
    }
}
