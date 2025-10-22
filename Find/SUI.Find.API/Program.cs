using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using SUI.Find.API.Endpoints;
using SUI.Find.API.Exceptions;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Services;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Fhir;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Services;
using Microsoft.AspNetCore.Http.Json;
using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();


// used sui matcher exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IAuthTokenService, StubAuthTokenService>();
else
    builder.Services.AddSingleton<IAuthTokenService, AuthTokenService>();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<AuthTokenServiceConfig>(builder.Configuration.GetSection("NhsAuthConfig"));

builder.Services.AddHttpClient("nhs-auth-api",
    client => { client.BaseAddress = new Uri(builder.Configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!); });
builder.Services.AddSingleton<SecretClient>(_ =>
{
    var keyVaultString = builder.Configuration.GetConnectionString("secrets") ??
                         throw new InvalidOperationException("Key Vault URI is not configured.");
    var uri = new Uri(keyVaultString);
    return new SecretClient(uri, new DefaultAzureCredential());
});

builder.Services.AddSingleton<ISecretService, SecretService>();
builder.Services.AddSingleton<IMatchingService, MatchingService>();
builder.Services.AddSingleton<IFhirService, FhirService>();
builder.Services.AddSingleton<IFhirClientFactory, FhirClientFactory>();
builder.Services.AddSingleton<ISearchIdService, SearchIdService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.UseExceptionHandler();

app.MapMatchEndpoints();

// configure http profile for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });
}

app.Run();

// Allow for setting up factory in integration tests
public partial class Program
{
}