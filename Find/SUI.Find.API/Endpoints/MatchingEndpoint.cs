using Microsoft.AspNetCore.Mvc;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUI.Find.Domain.Enums;

namespace SUI.Find.API.Endpoints;

public static class MatchingEndpoint
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var matchingGroup = app.MapGroup("api/v1/").WithTags("SUI Find API - Matching");
        matchingGroup.MapPost("/matchperson",
            async (PersonSpecification model, [FromServices] IMatchingService matchingService) =>
            {
                    var result = await matchingService.SearchAsync(model);

                    return result.Result?.MatchStatus == MatchStatus.Error
                        ? Results.BadRequest(result)
                        : Results.Ok(result);
            });
    }
}