namespace Oficina.Application.DTO.Seguranca;

public record LoginCpfRequest(string Cpf, string? Senha);

public sealed class AuthTokenResponse
{
    public required string AccessToken { get; init; }
    public required int ExpiresIn { get; init; }
    public required string Perfil { get; init; }
    public Guid? ClienteId { get; init; }
    public Guid? FuncionarioId { get; init; }
}
