namespace Oficina.Api.Security;

public sealed class AdminInicialBootstrapperAdapter(IServiceProvider services) : IAdminInicialBootstrapper
{
    public Task GarantirAdminInicial(CancellationToken ct)
        => AdminInicialBootstrapper.GarantirAdminInicial(services, ct);
}
