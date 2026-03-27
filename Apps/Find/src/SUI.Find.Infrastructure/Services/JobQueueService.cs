using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Factories;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public class JobQueueService(
    ILogger<JobQueueService> logger,
    IQueueClientFactory queueClientFactory
) : IJobQueueService
{
    public async Task<SearchWorkItemDto> PostSearchJobAsync(
        SearchRequestMessage payload,
        CancellationToken cancellationToken = default
    )
    {
        var queueClient = queueClientFactory.GetSearchJobClient();

        var messageJson = JsonSerializer.Serialize(payload);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        var base64Message = Convert.ToBase64String(messageBytes);

        try
        {
            await queueClient.SendMessageAsync(base64Message, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Search message posted to queue. WorkItemId: {WorkItemId}",
                    payload.WorkItemId.ToString()
                );

            var searchJob = new SearchWorkItemDto
            {
                WorkItemId = payload.WorkItemId.ToString(),
                PersonId = payload.PersonId,
                CreatedAt = DateTime.UtcNow,
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
