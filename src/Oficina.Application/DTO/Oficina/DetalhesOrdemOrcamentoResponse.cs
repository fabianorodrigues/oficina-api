namespace Oficina.Application.DTO.Oficina;

public sealed class ItemMaterialDetalheResponse
{
    public string Tipo { get; init; } = string.Empty;
    public Guid MaterialId { get; init; }
    public int Quantidade { get; init; }
    public decimal ValorUnitario { get; init; }
    public string? Descricao { get; init; }
}

public sealed class ItemServicoDetalheResponse
{
    public Guid ServicoId { get; init; }
    public decimal ValorMaoDeObra { get; init; }
}

public sealed class OrcamentoDetalheResponse
{
    public Guid Id { get; init; }
    public Guid OrdemServicoId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal ValorTotal { get; init; }
    public DateTimeOffset DataCriacao { get; init; }
    public IReadOnlyList<ItemServicoDetalheResponse> ItensServico { get; init; } = [];
    public IReadOnlyList<ItemMaterialDetalheResponse> ItensMaterial { get; init; } = [];
}

public sealed class DiagnosticoDetalheResponse
{
    public string Descricao { get; init; } = string.Empty;
    public DateTimeOffset DataRegistro { get; init; }
}

public sealed class OrdemServicoDetalheResponse
{
    public Guid Id { get; init; }
    public Guid VeiculoId { get; init; }
    public string TipoManutencao { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string OrigemUltimaAtualizacaoStatus { get; init; } = string.Empty;
    public DateTimeOffset DataUltimaAtualizacaoStatus { get; init; }
    public DateTimeOffset DataCriacao { get; init; }
    public DateTimeOffset? DataInicioExecucao { get; init; }
    public DateTimeOffset? DataFimExecucao { get; init; }
    public DiagnosticoDetalheResponse? Diagnostico { get; init; }
    public OrcamentoDetalheResponse? Orcamento { get; init; }
}
