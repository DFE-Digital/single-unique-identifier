using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using SUI.Find.Application.Constants;
using SUI.Find.Infrastructure.Clients;

namespace SUI.Find.Infrastructure.Factories;

public interface IQueueClientFactory
{
    /// <summary>
    /// Creates the required queues. If any queue already exists, it is not changed.
    /// </summary>
    /// <param name="cancellationToken">Optional token to propagate notifications that the operation should be cancelled.</param>
    /// <returns>Async operation task</returns>
    Task CreateQueuesIfNotExistsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a client for sending messages to the Audit queue.
    /// </summary>
    /// <returns>Audit queue client</returns>
    IAuditQueueSender GetAuditClient();

    /// <summary>
    /// Creates a client for sending messages to the Search Job queue.
    /// </summary>
    /// <returns>Search Job queue client</returns>
    ISearchJobQueueSender GetSearchJobClient();

    /// <summary>
    /// Creates a client for sending messages to the Job Results queue.
    /// </summary>
    /// <returns>Job Results queue client</returns>
    IJobResultsQueueSender GetJobResultsClient();
}

[ExcludeFromCodeCoverage(Justification = "Simple factory class")]
public class QueueClientFactory(IConfiguration config) : IQueueClientFactory
{
    private readonly string _auditConnectionString =
        config["AuditProcessorConnectionString"] ?? throw new InvalidOperationException();

    private readonly string _azureWebJobsStorageConnectionString =
        config["AzureWebJobsStorage"] ?? throw new InvalidOperationException();

    /// <inheritdoc/>
    public async Task CreateQueuesIfNotExistsAsync(CancellationToken cancellationToken)
    {
        await CreateAuditQueueClient().CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        await CreateSearchJobQueueClient()
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        await CreateJobResultsQueueClient()
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public IAuditQueueSender GetAuditClient() => new AzureQueueSender(CreateAuditQueueClient());

    /// <inheritdoc/>
    public ISearchJobQueueSender GetSearchJobClient() =>
        new AzureSearchJobQueueSender(CreateSearchJobQueueClient());

    /// <inheritdoc/>
    public IJobResultsQueueSender GetJobResultsClient() =>
        new AzureQueueSender(CreateJobResultsQueueClient());

    private QueueClient CreateAuditQueueClient() =>
        new(_auditConnectionString, ApplicationConstants.Audit.AccessQueueName);

    private QueueClient CreateSearchJobQueueClient() =>
        new(_azureWebJobsStorageConnectionString, ApplicationConstants.SearchJobs.QueueName);

    private QueueClient CreateJobResultsQueueClient() =>
        new(_azureWebJobsStorageConnectionString, ApplicationConstants.Jobs.JobResultsQueueName);
}
