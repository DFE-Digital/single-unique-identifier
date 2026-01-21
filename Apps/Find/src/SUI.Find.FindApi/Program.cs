using System.IO.Abstractions;
using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.Startup;
using SUI.Find.Infrastructure;
using SUI.Find.Infrastructure.Factories;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.Utility;

var builder = FunctionsApplication.CreateBuilder(args);

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// .NET services
builder.Services.AddSingleton(TimeProvider.System);

// Third-party and framework services
builder.Services.AddHealthChecks();
builder.Services.AddLogging();
builder.Services.AddSingleton<IFileSystem, FileSystem>();

// Custom application services
builder.Services.AddSingleton<IAuditService, AuditStorageTableService>();
builder.Services.AddSingleton<IFetchUrlStorageService, UrlStorageTableService>();
builder.Services.AddSingleton<IMaskUrlService, MaskUrlService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPersonIdEncryptionService, PersonIdEncryptionService>();
builder.Services.AddSingleton<IQueueClientFactory, QueueClientFactory>();
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<IFetchRecordService, FetchRecordService>();
builder.Services.AddSingleton<IHashService, HashService>();
builder.Services.AddSingleton<IQueryProvidersService, QueryProvidersService>();
builder.Services.AddSingleton<IProviderHttpClient, ProviderHttpClient>();
builder.Services.AddSingleton<IBuildCustodianRequestService, BuildCustodianRequestsService>();
builder.Services.AddSingleton<IBuildCustodianHttpRequest, BuildCustodianHttpRequest>();
builder.Services.AddSingleton<IPolicyEnforcementService, PolicyEnforcementService>();
builder.Services.AddSingleton<IAuditQueueClient, AuditQueueClient>();
builder.Services.AddAzureTableServices();

// Use mock services for all environments for now while in prototype
builder.Services.AddSingleton<IAuthStoreService, MockAuthStoreService>();
builder.Services.AddSingleton<ICustodianService, MockCustodianService>();
builder.Services.AddSingleton<IMatchRepository, MockMatchRepository>();
builder.Services.AddSingleton<IMatchingService, MatchingService>();
builder.Services.AddSingleton<IOutboundAuthService, OutboundAuthService>();

// Add this after other service registrations
builder.Services.AddHostedService<AzureStorageTableStartup>();

builder.UseMiddleware<JwtAuthMiddleware>();
builder.UseMiddleware<AuditMiddleware>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connection = config.GetValue<string>("AzureWebJobsStorage");

    return new TableServiceClient(connection);
});

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
