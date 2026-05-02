using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application.DTO.Seguranca;
using Oficina.Application.Shared;
using Oficina.Domain.Seguranca;
using Oficina.Domain.Seguranca.Enums;

namespace Oficina.Application.UseCases.Seguranca;

public class AutenticarFuncionarioUseCase
{
    private readonly IFuncionarioRepository _repo;
    private readonly IPasswordHashService _passwordHash;

    public AutenticarFuncionarioUseCase(IFuncionarioRepository repo, IPasswordHashService passwordHash)
    {
        _repo = repo;
        _passwordHash = passwordHash;
    }

    public async Task<Funcionario> Executar(string cpf, string senha, CancellationToken ct)
    {
        var cpfNormalizado = Funcionario.NormalizarCpf(cpf);
        var funcionario = await _repo.ObterPorCpf(cpfNormalizado, ct);

        if (funcionario is null || !funcionario.Ativo || !_passwordHash.Verificar(funcionario.SenhaHash, senha))
            throw new OficinaException("Credenciais invalidas.", 401);

        return funcionario;
    }
}

public class CriarFuncionarioUseCase
{
    private readonly IFuncionarioRepository _repo;
    private readonly IPasswordHashService _passwordHash;

    public CriarFuncionarioUseCase(IFuncionarioRepository repo, IPasswordHashService passwordHash)
    {
        _repo = repo;
        _passwordHash = passwordHash;
    }

    public async Task<FuncionarioResponse> Executar(CriarFuncionarioRequest request, CancellationToken ct)
    {
        var cpf = Funcionario.NormalizarCpf(request.Cpf);
        if (await _repo.ExistePorCpf(cpf, ct))
            throw new OficinaException("Funcionario ja cadastrado com este CPF.", 409);

        var perfil = ParsePerfil(request.Perfil);
        var funcionario = new Funcionario(request.Nome, cpf, _passwordHash.Hash(request.Senha), perfil);

        await _repo.Adicionar(funcionario, ct);
        await _repo.Salvar(ct);

        return Mapear(funcionario);
    }

    internal static PerfilUsuarioInterno ParsePerfil(string perfil)
        => Enum.TryParse<PerfilUsuarioInterno>(perfil, ignoreCase: true, out var valor)
            ? valor
            : throw new OficinaException("Perfil invalido.", 400);

    internal static FuncionarioResponse Mapear(Funcionario funcionario)
        => new()
        {
            Id = funcionario.Id,
            Nome = funcionario.Nome,
            Cpf = funcionario.Cpf,
            Perfil = funcionario.Perfil.ToString(),
            Ativo = funcionario.Ativo,
            DataCriacao = funcionario.DataCriacao
        };
}

public class ListarFuncionariosUseCase
{
    private readonly IFuncionarioRepository _repo;
    public ListarFuncionariosUseCase(IFuncionarioRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<FuncionarioResponse>> Executar(CancellationToken ct)
        => (await _repo.Listar(ct)).Select(CriarFuncionarioUseCase.Mapear).ToList();
}

public class ObterFuncionarioUseCase
{
    private readonly IFuncionarioRepository _repo;
    public ObterFuncionarioUseCase(IFuncionarioRepository repo) => _repo = repo;

    public async Task<FuncionarioResponse> Executar(Guid id, CancellationToken ct)
        => CriarFuncionarioUseCase.Mapear(await ObterEntidade(id, ct));

    internal async Task<Funcionario> ObterEntidade(Guid id, CancellationToken ct)
        => await _repo.ObterPorId(id, ct) ?? throw new OficinaException("Funcionario nao encontrado.", 404);
}

public class AtualizarFuncionarioUseCase
{
    private readonly IFuncionarioRepository _repo;
    private readonly ObterFuncionarioUseCase _obter;

    public AtualizarFuncionarioUseCase(IFuncionarioRepository repo, ObterFuncionarioUseCase obter)
    {
        _repo = repo;
        _obter = obter;
    }

    public async Task<FuncionarioResponse> Executar(Guid id, AtualizarFuncionarioRequest request, CancellationToken ct)
    {
        var funcionario = await _obter.ObterEntidade(id, ct);
        funcionario.Atualizar(request.Nome, CriarFuncionarioUseCase.ParsePerfil(request.Perfil), request.Ativo);
        await _repo.Salvar(ct);
        return CriarFuncionarioUseCase.Mapear(funcionario);
    }
}

public class AlterarSenhaFuncionarioUseCase
{
    private readonly IFuncionarioRepository _repo;
    private readonly ObterFuncionarioUseCase _obter;
    private readonly IPasswordHashService _passwordHash;

    public AlterarSenhaFuncionarioUseCase(
        IFuncionarioRepository repo,
        ObterFuncionarioUseCase obter,
        IPasswordHashService passwordHash)
    {
        _repo = repo;
        _obter = obter;
        _passwordHash = passwordHash;
    }

    public async Task Executar(Guid id, string novaSenha, CancellationToken ct)
    {
        var funcionario = await _obter.ObterEntidade(id, ct);
        funcionario.AlterarSenhaHash(_passwordHash.Hash(novaSenha));
        await _repo.Salvar(ct);
    }
}

public class AlterarStatusFuncionarioUseCase
{
    private readonly IFuncionarioRepository _repo;
    private readonly ObterFuncionarioUseCase _obter;

    public AlterarStatusFuncionarioUseCase(IFuncionarioRepository repo, ObterFuncionarioUseCase obter)
    {
        _repo = repo;
        _obter = obter;
    }

    public async Task Executar(Guid id, bool ativo, CancellationToken ct)
    {
        var funcionario = await _obter.ObterEntidade(id, ct);
        if (ativo) funcionario.Ativar();
        else funcionario.Inativar();
        await _repo.Salvar(ct);
    }
}
