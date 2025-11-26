using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using AuthenticationOptions = SUI.Transfer.Infrastructure.Authentication.AuthenticationOptions;

namespace SUI.Transfer.API.OpenApiTransformers;

[ExcludeFromCodeCoverage]
internal sealed class AuthenticationTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    private readonly string _headerName = "x-api-key";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (
            authenticationSchemes.Any(authScheme =>
                authScheme.Name == AuthenticationOptions.DefaultScheme
            )
        )
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

            document.Components.SecuritySchemes[_headerName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Scheme = _headerName,
                In = ParameterLocation.Header,
                BearerFormat = "String Token",
                Name = _headerName,
            };

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= [];
                operation.Value.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = _headerName,
                                },
                            }
                        ] = [],
                    }
                );
            }
        }
    }
}
