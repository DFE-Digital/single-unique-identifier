using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
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
            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[_headerName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Scheme = _headerName,
                In = ParameterLocation.Header,
                BearerFormat = "String Token",
                Name = _headerName,
            };

            var securitySchemeReference = new OpenApiSecuritySchemeReference(_headerName, document);

            foreach (var pathItem in document.Paths.Values)
            {
                if (pathItem.Operations == null)
                {
                    continue;
                }

                foreach (var operation in pathItem.Operations.Values)
                {
                    operation.Security ??= [];
                    operation.Security.Add(
                        new OpenApiSecurityRequirement { [securitySchemeReference] = [] }
                    );
                }
            }
        }
    }
}
