using Oficina.Api.Middlewares;

namespace Oficina.Api.Observability;

public sealed class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = ObterCorrelationId();
        if (!request.Headers.Contains(CorrelationIdMiddleware.HeaderName) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private string? ObterCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var item) &&
            item is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return httpContext.Request.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var values)
            ? values.FirstOrDefault()
            : null;
    }
}
