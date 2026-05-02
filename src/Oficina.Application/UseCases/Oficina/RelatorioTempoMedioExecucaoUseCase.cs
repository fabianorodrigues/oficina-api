using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;

namespace Oficina.Application.UseCases.Oficina;

public class RelatorioTempoMedioExecucaoUseCase
{
    private readonly IOficinaRepository _repo;
    public RelatorioTempoMedioExecucaoUseCase(IOficinaRepository repo) => _repo = repo;

    public async Task<RelatorioTempoMedioExecucaoResponse> Executar(CancellationToken ct)
    {
        var ordens = await _repo.ListarOrdensServico(ct);

        var duracoes = ordens
            .Where(o => o.DataInicioExecucao.HasValue && o.DataFimExecucao.HasValue)
            .Select(o => o.DataFimExecucao!.Value - o.DataInicioExecucao!.Value)
            .ToList();

        if (duracoes.Count == 0)
        {
            return new RelatorioTempoMedioExecucaoResponse
            {
                TempoMedio = null
            };
        }

        var mediaTicks = (long)duracoes.Average(d => d.Ticks);
        var media = TimeSpan.FromTicks(mediaTicks);

        return new RelatorioTempoMedioExecucaoResponse
        {
            TempoMedio = new TempoMedioExecucaoDetalheResponse
            {
                Dias = media.Days,
                Horas = media.Hours,
                Minutos = media.Minutes,
                Segundos = media.Seconds
            }
        };
    }
}
