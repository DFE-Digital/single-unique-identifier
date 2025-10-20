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
using DotNetEnv;


// TODO
// Testing
// Logging
// Error handles
// Correct API middleware
// Added helpers, interfaces etc in correct projects
// use of .env and settings correct?
// review ticket AC


// why do we use .env and appsettings

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to container
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();


// used sui matcher exception handler for now
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

if (builder.Environment.IsDevelopment()) builder.Services.AddSingleton<IAuthTokenService, StubAuthTokenService>();

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
// go through these with stuart
// AddTransient scope - in my mind we would want the scope to only live for the duration of the request
builder.Services.AddSingleton<ISecretService, SecretService>();
builder.Services.AddSingleton<IAuthTokenService, AuthTokenService>();
builder.Services.AddTransient<IMatchingService, MatchingService>();
builder.Services.AddTransient<IFhirService, FhirService>();
builder.Services.AddTransient<IFhirClientFactory, FhirClientFactory>();
builder.Services.AddTransient<ISearchIdService, SearchIdService>();

// will need to serialise json responses
// will need to add authentication and authorisation
var app = builder.Build();

app.UseExceptionHandler();

// This endpoint returns a 200 for a simple message.
app.MapGet("/test", () => new { Message = "Whats the matcher" });

app.MapMatchEndpoints(app.Services.GetRequiredService<IMatchingService>());

// configure http profile for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });
}

app.Run();