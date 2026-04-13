using System.IO.Abstractions;
using System.Net;
using Azure.Data.Tables;
using DotNetEnv;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Services;
using SUI.Find.Application.Services.Matching;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.Startup;
using SUI.Find.Infrastructure.Extensions;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models.Fhir;
using SUI.Find.Infrastructure.Services;

// Important this goes first before loading args. Localhost only concern
Env.TraversePath().Load();

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseOpenTelemetry();

// Options
var encryptionConfigExists = builder.Configuration.GetSection(EncryptionConfiguration.SectionName);
if (
    !encryptionConfigExists.Exists()
    || !bool.TryParse(
        encryptionConfigExists[nameof(EncryptionConfiguration.EnablePersonIdEncryption)],
        out _
    )
)
{
    throw new InvalidOperationException(
        $"Missing required configuration section: {EncryptionConfiguration.SectionName}"
    );
}
builder.Services.Configure<AuthTokenServiceConfig>(
    builder.Configuration.GetSection(AuthTokenServiceConfig.SectionName)
);

builder
    .Services.AddOptions<EncryptionConfiguration>()
    .BindConfiguration(EncryptionConfiguration.SectionName)
    .ValidateDataAnnotations();

builder
    .Services.AddOptions<MatchFunctionConfiguration>()
    .BindConfiguration(MatchFunctionConfiguration.SectionName)
    .ValidateDataAnnotations();

// .NET services
builder.Services.AddSingleton(TimeProvider.System);

// Third-party and framework services
builder.Services.AddHealthChecks();
builder.Services.AddLogging();
builder.Services.AddSingleton<IFileSystem, FileSystem>();

// Infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration).AddSecretClientServices();

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
builder.Services.AddSingleton<IMatchingEncryptionService, MatchingEncryptionService>();
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
