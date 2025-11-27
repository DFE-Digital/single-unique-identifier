using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sprache;
using SUI.Transfer.Application.Models;
using SUI.Transfer.Application.Services;

namespace SUI.Transfer.API.Endpoint;

public static class TransferEndpoint
{
    public static void MapTransferEndpoint(this IEndpointRouteBuilder app)
    {
        var transferGroup = app.MapGroup("api/v1/").WithTags("SUI Transfer API - Transfer");

        transferGroup
            .MapGet(
                "/transfer/{id}",
                [Authorize]
                async (
                    [Description(
                        "The single unique identifier for the data which is being requested."
                    )]
                        string id,
                    [FromServices] ITransferService transferService
                ) =>
                {
                    var result = await transferService.TransferAsync(id);

                    return HandleErrors(result);
                }
            )
            .WithSummary("Transfer custodian data for a given child")
            .WithDescription(
                "This endpoint requests external custodian systems for their data on a specific child, aggregates the data where necessary, and returns the data in a consolidated form."
            );
    }

    private static object HandleErrors(TransferResponse result)
    {
        // TODO - Propagate Custodian HTTP Errors up to this endpoint
        return Results.Ok(result.ConsolidatedData);
    }
}
