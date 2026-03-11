using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public class JobQueueService(
    ILogger<JobQueueService> logger,
    IConfiguration configuration,
    IHashService hashService
) : IJobQueueService
{
    private readonly string _connectionString =
        configuration["AzureWebJobsStorage"]
        ?? throw new InvalidOperationException("AzureWebJobsStorage is required");

    public async Task<SearchJobDto> PostSearchJobAsync(
        SearchRequestMessage payload,
        CancellationToken cancellationToken
    )
    {
        var queueClient = new QueueClient(
            _connectionString,
            ApplicationConstants.SearchJobs.QueueName
        );

        var messageJson = JsonSerializer.Serialize(payload);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        var base64Message = Convert.ToBase64String(messageBytes);

        var instanceId = $"{payload.PersonId}-{payload.RequestingCustodianId}";
        var hashedInstanceId = hashService.HmacSha256Hash(instanceId);
        try
        {
            await queueClient.SendMessageAsync(base64Message, cancellationToken);
            logger.LogInformation(
                "Search job posted to queue. WorkItemId: {WorkItemId}",
                payload.WorkItemId.ToString()
            );

            var searchJob = new SearchJobDto
            {
                JobId = payload.WorkItemId.ToString(),
                PersonId = payload.PersonId,
                Status = SearchStatus.Queued,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
            };

            return searchJob;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to post search job to queue. WorkItem: {WorkItemId}. Error: {ErrorMessage}",
                payload.WorkItemId.ToString(),
                ex.Message
            );
            throw;
        }
    }
}
