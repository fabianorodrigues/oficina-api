using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;

namespace Oficina.Application.UseCases.Seguranca;

public class AutenticarClienteUseCase
{
    private readonly ICadastroRepository _repo;

    public AutenticarClienteUseCase(ICadastroRepository repo) => _repo = repo;

    public async Task<Cliente> Executar(string cpf, CancellationToken ct)
    {
        var documento = new DocumentoCpfCnpj(cpf);
        if (documento.Valor.Length != 11)
            throw new OficinaException("Credenciais invalidas.", 401);

        return await _repo.ObterClientePorDocumento(documento.Valor, ct)
               ?? throw new OficinaException("Credenciais invalidas.", 401);
    }
}
