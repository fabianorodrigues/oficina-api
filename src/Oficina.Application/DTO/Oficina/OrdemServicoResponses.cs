namespace Oficina.Application.DTO.Oficina;

public sealed class CriarOsPreventivaResponse
{
    public Guid Id { get; init; }
    public Guid OrcamentoId { get; init; }
}

public sealed class CriarOsCorretivaResponse
{
    public Guid Id { get; init; }
}

public sealed class RegistrarDiagnosticoResponse
{
    public Guid OrcamentoId { get; init; }
}

public sealed class OrdemServicoListaItemResponse
{
    public Guid Id { get; init; }
    public Guid VeiculoId { get; init; }
    public string TipoManutencao { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset DataCriacao { get; init; }
}

