using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.Shared;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Application.DTO.Cadastro;

namespace Oficina.Application.UseCases.Cadastro;

public class AtualizarVeiculoUseCase
{
    private readonly ICadastroRepository _repo;

    public AtualizarVeiculoUseCase(ICadastroRepository repo) => _repo = repo;

    public async Task Executar(Guid id, string placa, string renavam, ModeloRequest modelo, CancellationToken ct)
    {
        var veiculo = await _repo.ObterVeiculo(id, ct);
        if (veiculo is null)
            throw new OficinaException("Veículo não encontrado.", 404);

        var novaPlaca = new Placa(placa);

        if (novaPlaca.Valor != veiculo.Placa.Valor &&
            await _repo.ExisteVeiculoPorPlaca(novaPlaca.Valor, ct))
            throw new OficinaException("Já existe veículo cadastrado com esta placa.", 409);

        veiculo.AlterarPlaca(novaPlaca);
        veiculo.AlterarRenavam(new Renavam(renavam));
        veiculo.AtualizarModelo(new Modelo(modelo.Descricao, modelo.Marca, modelo.Ano));

        await _repo.Salvar(ct);
    }
}
