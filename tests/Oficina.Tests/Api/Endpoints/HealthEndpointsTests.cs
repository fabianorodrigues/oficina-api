using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Oficina.Api.Endpoints;
using Xunit;

namespace Oficina.Tests.Api.Endpoints;

public class HealthEndpointsTests
{
    [Fact]
    public void Health_NaoDeveDependerDeBanco()
    {
        var result = HealthEndpoints.Health();

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Ready_QuandoBancoDisponivel_DeveRetornarOk()
    {
        var result = await HealthEndpoints.ReadyCore(
            _ => Task.FromResult(true),
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        var status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, status.StatusCode);
    }

    [Fact]
    public async Task Ready_QuandoBancoIndisponivel_DeveRetornarServiceUnavailable()
    {
        var result = await HealthEndpoints.ReadyCore(
            _ => Task.FromResult(false),
            TimeSpan.FromSeconds(1),
            CancellationToken.None);

        var status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }

    [Fact]
    public async Task Ready_QuandoBancoDemora_DeveUsarTimeoutCurto()
    {
        var result = await HealthEndpoints.ReadyCore(
            async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return true;
            },
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        var status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status.StatusCode);
    }
}
