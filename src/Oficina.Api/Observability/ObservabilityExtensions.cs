using Microsoft.Extensions.Http;
using Oficina.Application.Observability;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.Reflection;

namespace Oficina.Api.Observability;

public static class ObservabilityExtensions
{
    private const string DefaultServiceName = "oficina-api";
    private const string DefaultTraceSampler = "parentbased_traceidratio";
    private const double DefaultTraceSamplerArg = 0.1;

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
        var sampler = CreateTraceSampler();

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
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: ResolveServiceVersion())
                .AddAttributes(GetKubernetesResourceAttributes())
                .AddEnvironmentVariableDetector())
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(sampler)
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
                    .AddHttpClientInstrumentation()
                    .AddMeter(OficinaMetrics.MeterName);

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
            LogSafeWarning("OTEL_EXPORTER_OTLP_ENDPOINT is invalid. OTLP exporter disabled.");
            return false;
        }

        if (!TryGetOtlpProtocol(out var protocol))
        {
            LogSafeWarning("OTEL_EXPORTER_OTLP_PROTOCOL is invalid. OTLP exporter disabled.");
            return false;
        }

        settings = new OtlpExporterSettings(endpoint, protocol, GetOptionalOtlpHeaders());
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

    private static string? GetOptionalOtlpHeaders()
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

    private static Sampler CreateTraceSampler()
    {
        var samplerName = Environment.GetEnvironmentVariable("OTEL_TRACES_SAMPLER");
        if (string.IsNullOrWhiteSpace(samplerName))
            samplerName = DefaultTraceSampler;

        samplerName = samplerName.Trim().ToLowerInvariant();
        var samplerArg = GetTraceSamplerArg();

        return samplerName switch
        {
            "always_on" => new AlwaysOnSampler(),
            "always_off" => new AlwaysOffSampler(),
            "traceidratio" => new TraceIdRatioBasedSampler(samplerArg),
            "parentbased_traceidratio" => new ParentBasedSampler(new TraceIdRatioBasedSampler(samplerArg)),
            _ => UseDefaultSampler()
        };
    }

    private static Sampler UseDefaultSampler()
    {
        LogSafeWarning("OTEL_TRACES_SAMPLER is invalid. Using the default trace sampler.");
        return new ParentBasedSampler(new TraceIdRatioBasedSampler(DefaultTraceSamplerArg));
    }

    private static double GetTraceSamplerArg()
    {
        var value = Environment.GetEnvironmentVariable("OTEL_TRACES_SAMPLER_ARG");
        if (string.IsNullOrWhiteSpace(value))
            return DefaultTraceSamplerArg;

        if (double.TryParse(
                value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var samplerArg) &&
            samplerArg >= 0 &&
            samplerArg <= 1)
        {
            return samplerArg;
        }

        LogSafeWarning("OTEL_TRACES_SAMPLER_ARG is invalid. Using the default trace sampler argument.");
        return DefaultTraceSamplerArg;
    }

    private static void LogSafeWarning(string message)
    {
        Log.Warning(message);
        Console.WriteLine($"Warning: {message}");
    }

    private static string ResolveServiceVersion()
    {
        var configuredVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION");
        if (!string.IsNullOrWhiteSpace(configuredVersion))
            return configuredVersion;

        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                   ?? typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString()
                   ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static IEnumerable<KeyValuePair<string, object>> GetKubernetesResourceAttributes()
    {
        if (GetEnvironmentValue("K8S_POD_NAME") is { } podName)
            yield return new KeyValuePair<string, object>("k8s.pod.name", podName);

        if (GetEnvironmentValue("K8S_NAMESPACE") is { } namespaceName)
            yield return new KeyValuePair<string, object>("k8s.namespace.name", namespaceName);

        if (GetEnvironmentValue("K8S_NODE_NAME") is { } nodeName)
            yield return new KeyValuePair<string, object>("k8s.node.name", nodeName);
    }

    private static string? GetEnvironmentValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
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
