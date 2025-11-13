using Microsoft.AspNetCore.Mvc;
using SUI.Matching.Application.Interfaces;
using SUI.Matching.Application.Models;
using SUI.Matching.Domain.Enums;

namespace SUI.Matching.API.Endpoints;

public static class MatchingEndpoint
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var matchingGroup = app.MapGroup("api/v1/").WithTags("SUI Matching API - Matching");
        matchingGroup.MapPost(
            "/matchperson",
            async (PersonSpecification model, [FromServices] IMatchingService matchingService) =>
            {
                var result = await matchingService.SearchAsync(model);

                return result.Result?.MatchStatus == MatchStatus.Error
                    ? Results.BadRequest(result)
                    : Results.Ok(result);
            }
        );
    }
}
