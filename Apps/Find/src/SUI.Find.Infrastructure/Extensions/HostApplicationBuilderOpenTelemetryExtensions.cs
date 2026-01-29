using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace SUI.Find.Infrastructure.Extensions;

public static class HostApplicationBuilderOpenTelemetryExtensions
{
    public static IHostApplicationBuilder UseOpenTelemetry(this IHostApplicationBuilder builder)
    {
        const string AppInsightsConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
        });

        var appInsightsConnectionString = builder.Configuration[AppInsightsConnectionStringKey];
        var otlpEndpoint = builder.Configuration[OtlpEndpointKey];

        var telemetry = builder
            .Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddHttpClientInstrumentation())
            .UseFunctionsWorkerDefaults();

        var hasAppInsights = !string.IsNullOrWhiteSpace(appInsightsConnectionString);
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        if (hasAppInsights)
        {
            telemetry.UseAzureMonitorExporter();
        }

        if (hasOtlpEndpoint)
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }
}
