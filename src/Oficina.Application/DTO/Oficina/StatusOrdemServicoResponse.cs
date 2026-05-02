namespace Oficina.Application.DTO.Oficina;

public sealed class StatusOrdemServicoResponse
{
    public Guid OrdemServicoId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string OrigemUltimaAtualizacaoStatus { get; init; } = string.Empty;
    public DateTimeOffset DataUltimaAtualizacaoStatus { get; init; }
}
