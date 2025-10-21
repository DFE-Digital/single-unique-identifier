using Microsoft.AspNetCore.Mvc;
using SUI.Find.API.Exceptions;
// using SUI.Find.API.Validation;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUI.Find.Domain.Enums;

namespace SUI.Find.API.Endpoints;

public static class MatchEndpoint
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app, IMatchingService matchingService)
    {
        var matchingGroup = app.MapGroup("api/v1/").WithTags("SUI Find API - Matching");
        // can we move the functionality out of the endpoint registration
        matchingGroup.MapPost("/matchperson", async (SearchSpecification model) =>
        {
            try
            {
                var result = await matchingService.SearchAsync(model);

                return result.Result?.MatchStatus == MatchStatus.Error
                    ? Results.BadRequest(result)
                    : Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ProblemDetails
                    { Title = "Exception with Match Person", Detail = ex.Message });
            }
        });
    }
}