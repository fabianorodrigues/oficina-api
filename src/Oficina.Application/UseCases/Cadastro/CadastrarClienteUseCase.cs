using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;

namespace Oficina.Application.UseCases.Cadastro;

public class CadastrarClienteUseCase
{
    private readonly ICadastroRepository _repo;

    public CadastrarClienteUseCase(ICadastroRepository repo) => _repo = repo;

    public async Task<Guid> Executar(string cpfCnpj, string nome, string email, string telefone, CancellationToken ct)
    {
        var documento = new DocumentoCpfCnpj(cpfCnpj);

        if (await _repo.ExisteClientePorDocumento(documento.Valor, ct))
            throw new OficinaException("Cliente já cadastrado com este CPF/CNPJ.", 409);

        var contato = new Contato(email, telefone);

        var cliente = new Cliente(documento, nome, contato);
        await _repo.AdicionarCliente(cliente, ct);
        await _repo.Salvar(ct);

        return cliente.Id;
    }
}
