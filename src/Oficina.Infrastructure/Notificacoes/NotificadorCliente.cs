using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Email;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Infrastructure.Email.Configurations;
using Oficina.Infrastructure.Email.Templates;

namespace Oficina.Infrastructure.Notificacoes;

public class NotificadorCliente : INotificadorCliente
{
    private readonly ILogger<NotificadorCliente> _logger;
    private readonly IOficinaRepository _oficina;
    private readonly ICadastroRepository _cadastro;
    private readonly IEmailSender _emailSender;
    private readonly EmailSettings _emailSettings;

    public NotificadorCliente(
        ILogger<NotificadorCliente> logger,
        IOficinaRepository oficina,
        ICadastroRepository cadastro,
        IEmailSender emailSender,
        IOptions<EmailSettings> emailSettings)
    {
        _logger = logger;
        _oficina = oficina;
        _cadastro = cadastro;
        _emailSender = emailSender;
        _emailSettings = emailSettings.Value;
    }

    public async Task NotificarOrcamentoCriado(Guid orcamentoId, Guid ordemServicoId, CancellationToken ct)
    {
        var contexto = await ObterContextoNotificacao(orcamentoId, ct);
        if (contexto is null)
        {
            _logger.LogWarning("Não foi possível montar o e-mail do orçamento {OrcamentoId}: contexto incompleto.", orcamentoId);
            return;
        }

        var html = EmailOrcamentoTemplate.CriarHtml(contexto.Value.linkAprovar, contexto.Value.linkRecusar);
        await _emailSender.Enviar(new EmailMessage
        {
            To = contexto.Value.emailCliente,
            Subject = "Orçamento aguardando sua decisão",
            HtmlBody = html
        }, ct);

        _logger.LogInformation("Notificação por e-mail enviada para orçamento {OrcamentoId} e OS {OrdemServicoId}.", orcamentoId, ordemServicoId);
    }

    public Task NotificarOrcamentoRecusado(Guid orcamentoId, Guid ordemServicoId, CancellationToken ct)
    {
        _logger.LogInformation("Notificação: orçamento recusado {OrcamentoId} para OS {OrdemServicoId}. Cliente deve retirar o veículo.", orcamentoId, ordemServicoId);
        return Task.CompletedTask;
    }

    private async Task<(string emailCliente, string linkAprovar, string linkRecusar)?> ObterContextoNotificacao(Guid orcamentoId, CancellationToken ct)
    {
        var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct);
        if (orcamento?.TokenAcaoExterna is null) return null;

        var os = await _oficina.ObterOrdemServico(orcamento.OrdemServicoId, ct);
        if (os is null) return null;

        var veiculo = await _cadastro.ObterVeiculo(os.VeiculoId, ct);
        if (veiculo is null) return null;

        var cliente = await _cadastro.ObterCliente(veiculo.ClienteId, ct);
        if (cliente?.Contato?.Email is null) return null;

        var baseUrl = _emailSettings.BaseUrlAprovaRecusaOrcamento.TrimEnd('/');
        var token = Uri.EscapeDataString(orcamento.TokenAcaoExterna);
        return (
            cliente.Contato.Email,
            $"{baseUrl}/api/orcamentos/acoes-externas/aprovar?token={token}",
            $"{baseUrl}/api/orcamentos/acoes-externas/recusar?token={token}");
    }
}
