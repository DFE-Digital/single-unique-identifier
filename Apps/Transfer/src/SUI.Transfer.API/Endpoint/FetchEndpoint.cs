using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SUI.Transfer.Application.Services;

namespace SUI.Transfer.API.Endpoint;

public static class FetchEndpoint
{
    public static void MapFetchEndpoint(this IEndpointRouteBuilder app)
    {
        var fetchGroup = app.MapGroup("api/v1/").WithTags("SUI Transfer API - Fetch");

        fetchGroup
            .MapGet(
                "/fetch/{id}",
                [Authorize]
                async (
                    [Description(
                        "The single unique identifier for the data which is being requested."
                    )]
                        string id,
                    [FromServices] IFetchingService fetchingService
                ) =>
                {
                    var result = await fetchingService.FetchAsync(id);

                    return result.Success ? Results.Ok(result.Result) : Results.NotFound();
                }
            )
            .WithSummary("Fetch custodian data for a given child")
            .WithDescription(
                "This endpoint requests external custodian systems for their data on a specific child, aggregates the data where necessary, and returns the data in a consolidated form."
            );
    }
}
