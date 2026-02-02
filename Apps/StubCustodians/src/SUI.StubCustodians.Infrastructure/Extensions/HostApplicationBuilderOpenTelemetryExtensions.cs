using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SUI.StubCustodians.Infrastructure.Extensions;

public static class HostApplicationBuilderOpenTelemetryExtensions
{
    public static IHostApplicationBuilder UseOpenTelemetry(this IHostApplicationBuilder builder)
    {
        const string appInsightsConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";
        const string otlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
        });

        var appInsightsConnectionString = builder.Configuration[appInsightsConnectionStringKey];
        var otlpEndpoint = builder.Configuration[otlpEndpointKey];

        var openTelemetryBuilder = builder
            .Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation(options => options.RecordException = true);
            })
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("System.Net.NameResolution")
                    .AddMeter("System.Net.Security")
                    .AddMeter("System.Net.Sockets")
            );

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
