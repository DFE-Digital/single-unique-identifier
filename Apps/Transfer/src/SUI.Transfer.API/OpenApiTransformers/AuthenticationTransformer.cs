using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using AuthenticationOptions = SUI.Transfer.Infrastructure.Authentication.AuthenticationOptions;

namespace SUI.Transfer.API.OpenApiTransformers;

internal sealed class AuthenticationTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
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
            var securitySchemes = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["x-api-key"] = new()
                {
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "x-api-key",
                    In = ParameterLocation.Header,
                    BearerFormat = "String Token",
                    Name = "x-api-key",
                },
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;

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
                                    Id = "x-api-key",
                                },
                            }
                        ] = [],
                    }
                );
            }
        }
    }
}
