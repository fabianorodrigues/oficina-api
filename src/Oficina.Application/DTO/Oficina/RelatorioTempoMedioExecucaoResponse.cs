namespace Oficina.Application.DTO.Oficina;

public sealed class TempoMedioExecucaoDetalheResponse
{
    public int Dias { get; init; }
    public int Horas { get; init; }
    public int Minutos { get; init; }
    public int Segundos { get; init; }
}

public sealed class RelatorioTempoMedioExecucaoResponse
{
    public TempoMedioExecucaoDetalheResponse? TempoMedio { get; init; }
}

