using Microsoft.AspNetCore.Http;
using Oficina.Api.Middlewares;
using Oficina.Api.Observability;
using Xunit;

namespace Oficina.Tests.Api.Observability;

public class CorrelationIdDelegatingHandlerTests
{
    [Fact]
    public async Task DevePropagarCorrelationIdNoHttpClient()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        accessor.HttpContext.Items[CorrelationIdMiddleware.HeaderName] = "corr-456";

        var inner = new CaptureHandler();
        var handler = new CorrelationIdDelegatingHandler(accessor)
        {
            InnerHandler = inner
        };

        using var client = new HttpClient(handler);

        await client.GetAsync("https://example.com");

        Assert.Equal("corr-456", inner.CapturedRequest!.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
