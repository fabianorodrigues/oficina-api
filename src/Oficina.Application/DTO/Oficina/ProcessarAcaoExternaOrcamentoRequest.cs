namespace Oficina.Application.DTO.Oficina;

public enum AcaoExternaOrcamento
{
    Aprovar = 1,
    Recusar = 2
}

public class ProcessarAcaoExternaOrcamentoRequest
{
    public required string Token { get; init; }
    public AcaoExternaOrcamento Acao { get; init; }
}

public class ProcessarAcaoExternaOrcamentoResponse
{
    public bool Sucesso { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Mensagem { get; init; } = string.Empty;
    public Guid? OrcamentoId { get; init; }
    public Guid? OrdemServicoId { get; init; }
}
