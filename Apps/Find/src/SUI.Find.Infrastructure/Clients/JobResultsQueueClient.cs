using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Factories;

namespace SUI.Find.Infrastructure.Clients;

public class JobResultsQueueClient(
    ILogger<JobResultsQueueClient> logger,
    IQueueClientFactory queueClientFactory
) : IJobResultsQueueClient
{
    public async Task SendAsync(JobResultMessage message, CancellationToken cancellationToken)
    {
        var queueClient = queueClientFactory.GetJobResultsClient();

        var json = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);

        var bytes = Encoding.UTF8.GetBytes(json);

        var base64 = Convert.ToBase64String(bytes);

        try
        {
            await queueClient.SendMessageAsync(base64, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Job results posted to queue for JobId {JobId} by CustodianId {CustodianId}",
                    message.JobId,
                    message.CustodianId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to enqueue JobResultMessage for JobId {JobId} with CustodianId {CustodianId}",
                message.JobId,
                message.CustodianId
            );
        }
    }
}
