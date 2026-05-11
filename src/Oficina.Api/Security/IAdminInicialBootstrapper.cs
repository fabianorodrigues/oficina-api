namespace Oficina.Api.Security;

public interface IAdminInicialBootstrapper
{
    Task GarantirAdminInicial(CancellationToken ct);
}
