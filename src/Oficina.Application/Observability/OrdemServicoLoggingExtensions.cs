using Microsoft.Extensions.Logging;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.Observability;

public static class OrdemServicoLoggingExtensions
{
    public static void OrdemServicoCriada(this ILogger logger, OrdemServico os)
    {
        logger.LogInformation(
            "Ordem de servico criada {eventType} {ordemServicoId} {status}",
            OficinaEventTypes.OrdemServicoCriada,
            os.Id,
            os.Status.ToString());
    }

    public static void OrdemServicoStatusAlterado(
        this ILogger logger,
        OrdemServico os,
        StatusOrdemServico statusAnterior,
        DateTimeOffset dataStatusAnterior)
    {
        if (statusAnterior == os.Status)
            return;

        var statusDurationMs = CalcularDuracaoStatus(dataStatusAnterior, os.DataUltimaAtualizacaoStatus);
        if (statusDurationMs.HasValue)
        {
            logger.LogInformation(
                "Status da ordem de servico alterado {eventType} {ordemServicoId} {statusAnterior} {statusNovo} {status} {statusDurationMs}",
                OficinaEventTypes.OrdemServicoStatusAlterado,
                os.Id,
                statusAnterior.ToString(),
                os.Status.ToString(),
                os.Status.ToString(),
                statusDurationMs.Value);
            return;
        }

        logger.LogInformation(
            "Status da ordem de servico alterado {eventType} {ordemServicoId} {statusAnterior} {statusNovo} {status}",
            OficinaEventTypes.OrdemServicoStatusAlterado,
            os.Id,
            statusAnterior.ToString(),
            os.Status.ToString(),
            os.Status.ToString());
    }

    public static void OrdemServicoFalha(this ILogger logger, Exception ex, Guid? ordemServicoId = null)
    {
        if (ordemServicoId.HasValue)
        {
            logger.LogError(
                "Falha no processamento da ordem de servico {eventType} {ordemServicoId} {errorType}",
                OficinaEventTypes.OrdemServicoFalha,
                ordemServicoId.Value,
                ex.GetType().Name);
            return;
        }

        logger.LogError(
            "Falha no processamento da ordem de servico {eventType} {errorType}",
            OficinaEventTypes.OrdemServicoFalha,
            ex.GetType().Name);
    }

    private static long? CalcularDuracaoStatus(DateTimeOffset dataAnterior, DateTimeOffset dataAtual)
    {
        if (dataAnterior == default || dataAtual <= dataAnterior)
            return null;

        return Convert.ToInt64((dataAtual - dataAnterior).TotalMilliseconds);
    }
}
