namespace Oficina.Application.DTO.Seguranca;

public record LoginClienteRequest(string Cpf);
public record LoginFuncionarioRequest(string Cpf, string Senha);

public sealed class AuthTokenResponse
{
    public required string AccessToken { get; init; }
    public required string Perfil { get; init; }
    public Guid? ClienteId { get; init; }
    public Guid? FuncionarioId { get; init; }
}
