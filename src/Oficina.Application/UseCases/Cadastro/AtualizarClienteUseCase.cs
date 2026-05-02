using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Domain.Cadastro.ValueObjects;

namespace Oficina.Application.UseCases.Cadastro;

public class AtualizarClienteUseCase
{
    private readonly ICadastroRepository _repo;

    public AtualizarClienteUseCase(ICadastroRepository repo) => _repo = repo;

    public async Task Executar(Guid id, string cpfCnpj, string nome, string email, string telefone, CancellationToken ct)
    {
        var cliente = await _repo.ObterCliente(id, ct);
        if (cliente is null)
            throw new OficinaException("Cliente não encontrado.", 404);

        var novoDocumento = new DocumentoCpfCnpj(cpfCnpj);

        if (novoDocumento.Valor != cliente.Documento.Valor &&
            await _repo.ExisteClientePorDocumento(novoDocumento.Valor, ct))
            throw new OficinaException("Já existe cliente cadastrado com este CPF/CNPJ.", 409);

        var contato = new Contato(email, telefone);

        cliente.AlterarDocumento(novoDocumento);
        cliente.AlterarNome(nome);
        cliente.AlterarContato(contato);
        await _repo.Salvar(ct);
    }
}
