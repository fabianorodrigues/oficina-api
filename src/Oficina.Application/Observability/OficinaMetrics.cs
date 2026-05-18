using System.Diagnostics.Metrics;

namespace Oficina.Application.Observability;

public static class OficinaMetrics
{
    public const string MeterName = "Oficina.Business";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> OrdensServicoCriadas =
        Meter.CreateCounter<long>("ordens_servico_criadas");

    private static readonly Histogram<double> OrdemServicoProcessingMs =
        Meter.CreateHistogram<double>("ordem_servico_processing_ms", unit: "ms");

    private static readonly Counter<long> EmailsOrcamentoTentativas =
        Meter.CreateCounter<long>("emails_orcamento_tentativas");

    public static void RegistrarOrdemServicoCriada(string status)
    {
        OrdensServicoCriadas.Add(1, new KeyValuePair<string, object?>("status", status));
    }

    public static void RegistrarOrdemServicoProcessingMs(
        double durationMs,
        string statusAnterior,
        string statusNovo)
    {
        OrdemServicoProcessingMs.Record(
            durationMs,
            new KeyValuePair<string, object?>("statusAnterior", statusAnterior),
            new KeyValuePair<string, object?>("statusNovo", statusNovo));
    }

    public static void RegistrarEmailOrcamentoTentativa(string outcome, string? errorType = null)
    {
        if (string.IsNullOrWhiteSpace(errorType))
        {
            EmailsOrcamentoTentativas.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
            return;
        }

        EmailsOrcamentoTentativas.Add(
            1,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("errorType", errorType));
    }
}
