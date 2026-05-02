namespace Oficina.Application.Abstractions.Notificacoes;

// Notificação do orçamento (aprovar/recusar) e avisos de retirada
public interface INotificadorCliente
{
    Task NotificarOrcamentoCriado(Guid orcamentoId, Guid ordemServicoId, CancellationToken ct);
    Task NotificarOrcamentoRecusado(Guid orcamentoId, Guid ordemServicoId, CancellationToken ct);
}
