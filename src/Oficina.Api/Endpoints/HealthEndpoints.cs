using Microsoft.AspNetCore.Http.HttpResults;
using Oficina.Infrastructure.Persistencia;

namespace Oficina.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(2);

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", Health).AllowAnonymous();
        endpoints.MapGet("/ready", Ready).AllowAnonymous();

        return endpoints;
    }

    public static Ok<object> Health()
        => TypedResults.Ok<object>(new { status = "Healthy" });

    public static Task<IResult> Ready(OficinaDbContext dbContext, CancellationToken ct)
        => ReadyCore(token => dbContext.Database.CanConnectAsync(token), ReadyTimeout, ct);

    public static async Task<IResult> ReadyCore(
        Func<CancellationToken, Task<bool>> canConnect,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            if (await canConnect(timeoutCts.Token))
                return TypedResults.Ok<object>(new { status = "Ready" });
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
