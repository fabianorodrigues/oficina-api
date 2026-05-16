using Microsoft.Extensions.Http;
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
        var otlpEnabled = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

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
                    tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (otlpEnabled)
                    metrics.AddOtlpExporter();
            });

        return services;
    }
}
