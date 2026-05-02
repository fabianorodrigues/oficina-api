namespace Oficina.Api.Security;

public interface IUsuarioAtual
{
    Guid ClienteId { get; }
}
