using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SUI.Find.Infrastructure.Extensions;

public static class HostApplicationBuilderOpenTelemetryExtensions
{
    public static IHostApplicationBuilder UseOpenTelemetry(this IHostApplicationBuilder builder)
    {
        const string appInsightsConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        const string otlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
        const string otelServiceNameKey = "OTEL_SERVICE_NAME";

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
        });

        var appInsightsConnectionString = builder.Configuration[appInsightsConnectionStringKey];
        var otlpEndpoint = builder.Configuration[otlpEndpointKey];
        var configuredServiceName = builder.Configuration[otelServiceNameKey];

        var openTelemetryBuilder = builder.Services.AddOpenTelemetry();

        var serviceName = string.IsNullOrWhiteSpace(configuredServiceName)
            ? builder.Environment.ApplicationName
            : configuredServiceName;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            openTelemetryBuilder.ConfigureResource(resource => resource.AddService(serviceName));
        }

        openTelemetryBuilder
            .WithTracing(tracing =>
            {
                tracing
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddSource("Azure.Data.Tables")
                    .AddSource("Azure.Security.KeyVault.Secrets")
                    .AddSource("Azure.Storage.Queues");
            })
            .WithMetrics(metrics =>
                metrics
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("System.Net.NameResolution")
                    .AddMeter("System.Net.Security")
                    .AddMeter("System.Net.Sockets")
            )
            .UseFunctionsWorkerDefaults();

        var hasAppInsights = !string.IsNullOrWhiteSpace(appInsightsConnectionString);
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        if (hasAppInsights)
        {
            openTelemetryBuilder.UseAzureMonitorExporter();
        }

        if (hasOtlpEndpoint)
        {
            openTelemetryBuilder.UseOtlpExporter();
        }

        return builder;
    }
}
