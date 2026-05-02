using Oficina.Application.Shared;

namespace Oficina.Api.Security;

public class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioAtual(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid ClienteId
    {
        get
        {
            var valor = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimsOficina.ClienteId)?.Value;
            return Guid.TryParse(valor, out var id)
                ? id
                : throw new OficinaException("Token de cliente invalido.", 403);
        }
    }
}
