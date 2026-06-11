using System.IO.Abstractions;
using System.Net;
using Azure.Data.Tables;
using DotNetEnv;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Polly;
using Polly.Extensions.Http;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Services;
using SUI.Find.Application.Services.Matching;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.OpenApi;
using SUI.Find.FindApi.Startup;
using SUI.Find.Infrastructure.Extensions;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models.Fhir;
using SUI.Find.Infrastructure.Services;

// Important this goes first before loading args. Localhost only concern
Env.TraversePath().Load();

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseOpenTelemetry();

builder.Services.Configure<AuthTokenServiceConfig>(
    builder.Configuration.GetSection(AuthTokenServiceConfig.SectionName)
);

// Bind AuthSettings to configuration
builder.Services.Configure<AuthSettings>(
    builder.Configuration.GetSection(AuthSettings.SectionName)
);

builder
    .Services.AddOptions<MatchFunctionConfiguration>()
    .BindConfiguration(MatchFunctionConfiguration.SectionName)
    .ValidateDataAnnotations();

// .NET services
builder.Services.AddSingleton(TimeProvider.System);

// Register OpenID Connect ConfigurationManager as a Singleton to cache public keys across function invocations
builder.Services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<AuthSettings>>().Value;
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
builder.Services.AddSingleton<IOpenApiConfigurationOptions, FindOpenApiOptions>();

// Infrastructure services
builder.Services.AddInfrastructureServices();

// Middleware services
builder.Services.AddSingleton<IAuthContextFactory, AuthContextFactory>();

// Application services
builder.Services.AddSingleton<IMaskUrlService, MaskUrlService>();
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<IFetchRecordService, FetchRecordService>();
builder.Services.AddSingleton<IQueryProvidersService, QueryProvidersService>();
builder.Services.AddSingleton<IPolicyEnforcementService, PolicyEnforcementService>();
builder.Services.AddSingleton<ISearchResultsService, SearchResultsService>();
builder.Services.AddSingleton<IMatchPersonOrchestrationService, MatchPersonOrchestrationService>();
builder.Services.AddSingleton<IMatchingService, MatchingService>();
builder.Services.AddSingleton<IJobQueueService, JobQueueService>();
builder.Services.AddPdsSearchStrategies();

// Use mock services for all environments for now while in prototype
builder.Services.AddSingleton<IAuthStoreService, MockAuthStoreService>();
builder.Services.AddSingleton<ICustodianService, MockCustodianService>();
builder.Services.AddSingleton<IMatchRepository, MockMatchRepository>();
builder.Services.AddSingleton<IOutboundAuthService, OutboundAuthService>();

// Add this after other service registrations
builder.Services.AddHostedService<AzureStorageTableStartup>();
builder.Services.AddHostedService<AzureStorageQueueStartup>();

builder.UseMiddleware<JwtAuthMiddleware>();
builder.UseMiddleware<AuditMiddleware>();
builder.UseMiddleware<ResponseTracingMiddleware>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connection = config.GetValue<string>("AzureWebJobsStorage");

    return new TableServiceClient(connection);
});

builder.Services.AddHttpClient(
    "nhs-auth-api",
    client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["NhsAuthConfig:NHS_DIGITAL_TOKEN_URL"]!);
    }
);

builder
    .Services.AddHttpClient(ApplicationConstants.Providers.LoggingName)
    .AddPolicyHandler(_ =>
    {
        var logger = builder
            .Services.BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(ApplicationConstants.Providers.LoggingName);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogInformation(
                        "Retrying after {TotalSeconds} seconds (attempt {RetryCount})",
                        timespan.TotalSeconds,
                        retryCount
                    );
                }
            );
    });

await builder.Build().RunAsync();
