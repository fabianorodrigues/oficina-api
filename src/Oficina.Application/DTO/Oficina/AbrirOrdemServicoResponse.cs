namespace Oficina.Application.DTO.Oficina;

public sealed class AbrirOrdemServicoResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
}
