using Microsoft.AspNetCore.Mvc;
using SUI.Transfer.Application.Services;

namespace SUI.Transfer.API.Endpoint;

public static class FetchEndpoint
{
    public static void MapFetchEndpoint(this IEndpointRouteBuilder app)
    {
        var fetchGroup = app.MapGroup("api/v1/").WithTags("SUI Transfer API - Fetch");
        fetchGroup.MapGet("/fetch/{id}",
            async (string id, [FromServices] IFetchingService fetchingService) =>
            {
                var result = await fetchingService.FetchAsync(id);

                return result.Success 
                    ? Results.Ok(result.Result) 
                    : Results.NotFound();
            });
    }
}