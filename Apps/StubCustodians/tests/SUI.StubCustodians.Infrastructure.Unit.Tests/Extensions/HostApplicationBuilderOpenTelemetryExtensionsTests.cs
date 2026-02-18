using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using Shouldly;
using SUI.StubCustodians.Infrastructure.Extensions;

namespace SUI.StubCustodians.Infrastructure.Unit.Tests.Extensions;

public class HostApplicationBuilderOpenTelemetryExtensionsTests
{
    [Fact]
    public void UseOpenTelemetry_registers_exporters_when_both_configured()
    {
        var builder = CreateBuilder(
            new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
                    "InstrumentationKey=00000000-0000-0000-0000-000000000000",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
            }
        );

        builder.UseOpenTelemetry();

        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<AzureMonitorExporterOptions>)
        );
        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IOptionsFactory<OtlpExporterOptions>)
        );
    }

    [Fact]
    public void UseOpenTelemetry_registers_only_azure_monitor_when_configured()
    {
        var builder = CreateBuilder(
            new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] =
                    "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            }
        );

        builder.UseOpenTelemetry();

        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<AzureMonitorExporterOptions>)
        );
        builder.Services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(IOptionsFactory<OtlpExporterOptions>)
        );
    }

    [Fact]
    public void UseOpenTelemetry_registers_only_otlp_when_configured()
    {
        var builder = CreateBuilder(
            new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
            }
        );

        builder.UseOpenTelemetry();

        builder.Services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<AzureMonitorExporterOptions>)
        );
        builder.Services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IOptionsFactory<OtlpExporterOptions>)
        );
    }

    [Fact]
    public void UseOpenTelemetry_skips_exporters_when_not_configured()
    {
        var builder = CreateBuilder(new Dictionary<string, string?>());

        builder.UseOpenTelemetry();

        builder.Services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<AzureMonitorExporterOptions>)
        );
        builder.Services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(IOptionsFactory<OtlpExporterOptions>)
        );
    }

    private static HostApplicationBuilder CreateBuilder(Dictionary<string, string?> values)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(values);

        return builder;
    }
}
