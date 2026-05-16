using Oficina.Api.Observability;

namespace Oficina.Api.Middlewares;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context, ICorrelationContext correlationContext)
    {
        var correlationId = ObterCorrelationId(context);
        correlationContext.CorrelationId = correlationId;
        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }

    private static string ObterCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var value = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
