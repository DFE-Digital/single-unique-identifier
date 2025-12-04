using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
                "This endpoint begins a job that requests external custodian systems for their data on a specific child, aggregates the data where necessary, and returns the data in a consolidated form."
            );

        transferGroup.MapDelete(
            "/transfer/{jobId}",
            [Authorize]
            Ok<CancelledTransferJobState> (
                [Description("The guid id of the job being cancelled.")] Guid jobId
            ) => throw new NotImplementedException("To be implemented by SUI-1271")
        );
    }
}
