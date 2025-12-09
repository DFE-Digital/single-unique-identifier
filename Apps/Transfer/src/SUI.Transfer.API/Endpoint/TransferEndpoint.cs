using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SUI.Transfer.Application.Models;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.API.Endpoint;

public static class TransferEndpoint
{
    public static void MapTransferEndpoint(this IEndpointRouteBuilder app)
    {
        var transferGroup = app.MapGroup("api/v1/").WithTags("SUI Transfer API - Transfer");

        transferGroup
            .MapPost(
                "/transfer",
                [Authorize]
                Ok<QueuedTransferJobState> (
                    [Description(
                        "The single unique identifier for the data which is being requested."
                    )]
                    [FromBody]
                        string sui,
                    [FromServices] ITransferService transferService
                ) =>
                {
                    var result = transferService.BeginTransferJob(sui);

                    return TypedResults.Ok(result);
                }
            )
            .WithSummary("Transfer custodian data for a given child")
            .WithDescription(
                "This endpoint begins a job that requests external custodian systems for their data on a specific child."
            );

        transferGroup
            .MapGet(
                "/transfer/{jobId}",
                [Authorize]
                async Task<Results<Ok<TransferJobState>, NotFound>> (
                    [Description("The guid id of the job whose status is being requested.")]
                        Guid jobId,
                    [FromServices] ITransferService transferService
                ) =>
                {
                    var result = await transferService.GetTransferJobStateAsync(jobId);

                    if (result is null)
                        return TypedResults.NotFound();

                    return TypedResults.Ok(
                        result.Status == TransferJobStatus.Completed
                            ? TransferJobStateFactory.CompleteJob(
                                result,
                                null,
                                result.LastUpdatedAt
                            ) // avoid sending results from status endpoint
                            : result
                    );
                }
            )
            .WithSummary("Get Status of Transfer job")
            .WithDescription("This endpoint returns the status of a given transfer job.");

        transferGroup
            .MapGet(
                "/transfer/{jobId}/results",
                [Authorize]
                async Task<
                    Results<Ok<CompletedTransferJobState>, BadRequest<TransferJobState>, NotFound>
                > (
                    [Description("The guid id of the job whose results are being requested.")]
                        Guid jobId,
                    [FromServices] ITransferService transferService
                ) =>
                {
                    var jobState = await transferService.GetTransferJobStateAsync(jobId);
                    if (jobState is null)
                        return TypedResults.NotFound();

                    if (jobState is CompletedTransferJobState completedJobState)
                        return TypedResults.Ok(completedJobState);

                    return TypedResults.BadRequest(jobState);
                }
            )
            .WithSummary("Get Result of Transfer Job")
            .WithDescription("This endpoint returns the results of a transfer job, if completed.");

        transferGroup
            .MapDelete(
                "/transfer/{jobId}",
                [Authorize]
                Ok<CancelledTransferJobState> (
                    [Description("The guid id of the job being cancelled.")] Guid jobId
                ) => throw new NotImplementedException("To be implemented by SUI-1271")
            )
            .WithSummary("Cancel Transfer Job")
            .WithDescription("This endpoint cancels a given transfer job, if not yet completed.");
    }
}
