namespace Oficina.Application.DTO.Seguranca;

public record CriarFuncionarioRequest(string Nome, string Cpf, string Senha, string Perfil);
public record AtualizarFuncionarioRequest(string Nome, string Perfil, bool Ativo);
public record AlterarSenhaFuncionarioRequest(string NovaSenha);

public sealed class FuncionarioResponse
{
    public Guid Id { get; init; }
    public required string Nome { get; init; }
    public required string Cpf { get; init; }
    public required string Perfil { get; init; }
    public bool Ativo { get; init; }
    public DateTimeOffset DataCriacao { get; init; }
}
