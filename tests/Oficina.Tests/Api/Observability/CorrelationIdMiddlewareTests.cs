using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Api.Middlewares;
using Oficina.Api.Observability;
using Xunit;

namespace Oficina.Tests.Api.Observability;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task DeveUsarCorrelationIdRecebidoEDevolverNaResposta()
    {
        const string correlationId = "corr-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        var correlationContext = new CorrelationContext();
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context, correlationContext);
        await context.Response.StartAsync();

        Assert.Equal(correlationId, correlationContext.CorrelationId);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task DeveGerarCorrelationIdQuandoHeaderNaoVier()
    {
        var context = new DefaultHttpContext();
        var correlationContext = new CorrelationContext();
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.Invoke(context, correlationContext);
        await context.Response.StartAsync();

        Assert.False(string.IsNullOrWhiteSpace(correlationContext.CorrelationId));
        Assert.Equal(correlationContext.CorrelationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }
}
