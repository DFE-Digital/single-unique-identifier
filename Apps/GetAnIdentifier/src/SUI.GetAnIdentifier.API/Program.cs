using System.IO.Abstractions;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SUI.GetAnIdentifier.API.Configuration;
using SUI.GetAnIdentifier.API.Middleware;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Services;
using SUI.GetAnIdentifier.Infrastructure;
using SUI.GetAnIdentifier.Infrastructure.Factories;
using SUI.GetAnIdentifier.Infrastructure.Interfaces;
using SUI.GetAnIdentifier.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.Configure<AuthTokenServiceConfig>(
    builder.Configuration.GetSection(AuthTokenServiceConfig.SectionName)
);

// Bind AuthSettings to configuration
builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection(AuthSettings.SectionName)
);

builder
    .Services.AddOptions<GetAnIdentifierConfiguration>()
    .BindConfiguration(GetAnIdentifierConfiguration.SectionName)
    .ValidateDataAnnotations();

// Register OpenID Connect ConfigurationManager as a Singleton to cache public keys across function invocations
builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<AuthSettings>>().Value;

    var logger = sp.GetRequiredService<ILogger<Program>>();
    if (logger.IsEnabled(LogLevel.Information))
        logger.LogInformation(
            "Using OIDC Discovery URL: {OidcDiscoveryUrl}",
            settings.OidcDiscoveryUrl
        );

    return new ConfigurationManager<OpenIdConnectConfiguration>(
        settings.OidcDiscoveryUrl,
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever { RequireHttps = true }
    );
});

// Third-party and framework services
builder.Services.AddHealthChecks();
builder.Services.AddLogging();
builder.Services.AddSingleton<IFileSystem, FileSystem>();

builder.Services.AddSingleton(x =>
{
    var connectionString =
        builder.Configuration["AzureWebJobsStorage"]
        ?? throw new ArgumentNullException(builder.Configuration["AzureWebJobsStorage"]);
    var containerName =
        builder.Configuration["AuditStorage:ContainerName"]
        ?? throw new ArgumentNullException(builder.Configuration["AuditStorage:ContainerName"]);
    return new BlobContainerClient(connectionString, containerName);
});

// Infrastructure services
builder.Services.AddSingleton<IFhirClientFactory, FhirClientFactory>();
builder.Services.AddSingleton<IFhirService, FhirService>();
builder.Services.AddSingleton<IFhirAuthTokenService, FhirAuthTokenService>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

// Middleware services
builder.Services.AddSingleton<IAuthContextFactory, AuthContextFactory>();

// Application services
builder.Services.AddSingleton<IGetAnIdentifierService, GetAnIdentifierService>();

// Use mock services for all environments for now while in prototype
builder.Services.AddSingleton<IAuthStoreService, MockAuthStoreService>();

builder.UseMiddleware<JwtAuthMiddleware>();

builder.Services.AddHttpClient(
    "nhs-auth-api",
    client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!);
    }
);

await builder.Build().RunAsync();
