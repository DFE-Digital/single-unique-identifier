using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Http.Json;
using SUI.Transfer.API.Endpoint;
using SUI.Transfer.API.OpenApiTransformers;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Infrastructure.Authentication;
using AuthenticationOptions = SUI.Transfer.Infrastructure.Authentication.AuthenticationOptions;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<AuthenticationTransformer>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

builder
    .Services.AddAuthentication(AuthenticationOptions.DefaultScheme)
    .AddScheme<AuthenticationOptions, ApiKeyAuthenticationHandler>(
        AuthenticationOptions.DefaultScheme,
        _ => { }
    );

builder.Services.AddAuthorizationBuilder();

builder.Services.AddSingleton<ITransferService, TransferService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    );
});

var app = builder.Build();

app.UseExceptionHandler();

// configure http profile for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapTransferEndpoint();

app.UseHttpsRedirection();

await app.RunAsync();

// Allow for setting up factory in tests
public partial class Program { }
