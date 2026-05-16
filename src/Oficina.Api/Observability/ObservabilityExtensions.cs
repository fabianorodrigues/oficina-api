using Microsoft.Extensions.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace Oficina.Api.Observability;

public static class ObservabilityExtensions
{
    private const string DefaultServiceName = "oficina-api";

    public static void ConfigureStructuredLogging(this WebApplicationBuilder builder)
    {
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? DefaultServiceName;

        builder.Host.UseSerilog((context, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service.name", serviceName)
                .WriteTo.Console(new CompactJsonFormatter());
        });
    }

    public static IServiceCollection AddOficinaObservability(this IServiceCollection services)
    {
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? DefaultServiceName;
        var otlpEnabled = TryCreateOtlpExporterSettings(out var otlpSettings);

        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddHttpClient();
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<CorrelationIdDelegatingHandler>());
            });
        });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEnabled)
                    tracing.AddOtlpExporter(options => ConfigureOtlpExporter(options, otlpSettings!, "v1/traces"));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEnabled)
                    metrics.AddOtlpExporter(options => ConfigureOtlpExporter(options, otlpSettings!, "v1/metrics"));
            });

        return services;
    }

    private static bool TryCreateOtlpExporterSettings(out OtlpExporterSettings? settings)
    {
        settings = null;

        var endpointValue = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpointValue))
            return false;

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint) ||
            string.IsNullOrWhiteSpace(endpoint.Host) ||
            (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        if (!TryGetOtlpProtocol(out var protocol))
            return false;

        settings = new OtlpExporterSettings(endpoint, protocol, GetValidOtlpHeaders());
        return true;
    }

    private static bool TryGetOtlpProtocol(out OtlpExportProtocol protocol)
    {
        var value = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        if (string.IsNullOrWhiteSpace(value))
        {
            protocol = OtlpExportProtocol.HttpProtobuf;
            return true;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "http/protobuf":
                protocol = OtlpExportProtocol.HttpProtobuf;
                return true;
            case "grpc":
                protocol = OtlpExportProtocol.Grpc;
                return true;
            default:
                protocol = OtlpExportProtocol.HttpProtobuf;
                return false;
        }
    }

    private static string? GetValidOtlpHeaders()
    {
        var headers = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
        if (string.IsNullOrWhiteSpace(headers))
            return null;

        var pairs = headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pairs.Length == 0)
            return null;

        return pairs.All(pair =>
        {
            var separatorIndex = pair.IndexOf('=');
            return separatorIndex > 0 && separatorIndex < pair.Length - 1;
        })
            ? headers
            : null;
    }

    private static void ConfigureOtlpExporter(
        OtlpExporterOptions options,
        OtlpExporterSettings settings,
        string httpProtobufSignalPath)
    {
        options.Endpoint = BuildSignalEndpoint(settings, httpProtobufSignalPath);
        options.Protocol = settings.Protocol;

        if (!string.IsNullOrWhiteSpace(settings.Headers))
            options.Headers = settings.Headers;
    }

    private static Uri BuildSignalEndpoint(OtlpExporterSettings settings, string httpProtobufSignalPath)
    {
        if (settings.Protocol == OtlpExportProtocol.Grpc)
            return settings.Endpoint;

        var builder = new UriBuilder(settings.Endpoint);
        var basePath = builder.Path?.Trim('/');
        builder.Path = string.IsNullOrWhiteSpace(basePath)
            ? httpProtobufSignalPath
            : $"{basePath}/{httpProtobufSignalPath}";

        return builder.Uri;
    }

    private sealed record OtlpExporterSettings(
        Uri Endpoint,
        OtlpExportProtocol Protocol,
        string? Headers);
}
