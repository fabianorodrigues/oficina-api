using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Shared;

namespace Oficina.Application.UseCases.Oficina;

public class ObterStatusOrdemServicoUseCase
{
    private readonly IOficinaRepository _repo;

    public ObterStatusOrdemServicoUseCase(IOficinaRepository repo)
    {
        _repo = repo;
    }

    public async Task<StatusOrdemServicoResponse> Executar(Guid ordemServicoId, CancellationToken ct)
    {
        var os = await _repo.ObterOrdemServico(ordemServicoId, ct)
                 ?? throw new OficinaException("Ordem de serviço não encontrada.", 404);

        return new StatusOrdemServicoResponse
        {
            OrdemServicoId = os.Id,
            Status = os.Status.ToString(),
            OrigemUltimaAtualizacaoStatus = os.OrigemUltimaAtualizacaoStatus.ToString(),
            DataUltimaAtualizacaoStatus = os.DataUltimaAtualizacaoStatus
        };
    }
}
