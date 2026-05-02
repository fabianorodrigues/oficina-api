using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Email;
using Oficina.Application.Abstractions.Notificacoes;
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
            return;
        }

        try
        {
            var html = EmailOrcamentoTemplate.CriarHtml(contexto.Value.linkAprovar, contexto.Value.linkRecusar);
            await _emailSender.Enviar(new EmailMessage
            {
                To = contexto.Value.emailCliente,
                Subject = "Orcamento aguardando sua decisao",
                HtmlBody = html
            }, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail do orcamento {OrcamentoId} para {EmailCliente}. A acao principal foi preservada.", orcamentoId, contexto.Value.emailCliente);
            return;
        }

        _logger.LogInformation("Notificacao por e-mail enviada para orcamento {OrcamentoId} e OS {OrdemServicoId}.", orcamentoId, ordemServicoId);
    }

    public Task NotificarOrcamentoRecusado(Guid orcamentoId, Guid ordemServicoId, CancellationToken ct)
    {
        _logger.LogInformation("Notificacao: orcamento recusado {OrcamentoId} para OS {OrdemServicoId}. Cliente deve retirar o veiculo.", orcamentoId, ordemServicoId);
        return Task.CompletedTask;
    }

    private async Task<(string emailCliente, string linkAprovar, string linkRecusar)?> ObterContextoNotificacao(Guid orcamentoId, CancellationToken ct)
    {
        var orcamento = await _oficina.ObterOrcamento(orcamentoId, ct);
        if (orcamento is null)
        {
            _logger.LogWarning("Nao foi possivel montar o e-mail do orcamento {OrcamentoId}: orcamento nao encontrado.", orcamentoId);
            return null;
        }

        if (orcamento.TokenAcaoExterna is null)
        {
            _logger.LogWarning("Nao foi possivel montar o e-mail do orcamento {OrcamentoId}: token de acao externa ausente.", orcamentoId);
            return null;
        }

        var os = await _oficina.ObterOrdemServico(orcamento.OrdemServicoId, ct);
        if (os is null)
        {
            _logger.LogWarning("Nao foi possivel montar o e-mail do orcamento {OrcamentoId}: ordem de servico {OrdemServicoId} nao encontrada.", orcamentoId, orcamento.OrdemServicoId);
            return null;
        }

        var veiculo = await _cadastro.ObterVeiculo(os.VeiculoId, ct);
        if (veiculo is null)
        {
            _logger.LogWarning("Nao foi possivel montar o e-mail do orcamento {OrcamentoId}: veiculo {VeiculoId} nao encontrado.", orcamentoId, os.VeiculoId);
            return null;
        }

        var cliente = await _cadastro.ObterCliente(veiculo.ClienteId, ct);
        if (cliente is null)
        {
            _logger.LogWarning("Nao foi possivel montar o e-mail do orcamento {OrcamentoId}: cliente {ClienteId} nao encontrado.", orcamentoId, veiculo.ClienteId);
            return null;
        }

        if (cliente.Contato?.Email is null)
        {
            _logger.LogWarning("Nao foi possivel montar o e-mail do orcamento {OrcamentoId}: cliente {ClienteId} sem e-mail cadastrado.", orcamentoId, cliente.Id);
            return null;
        }

        var baseUrl = _emailSettings.BaseUrlAprovaRecusaOrcamento.TrimEnd('/');
        var token = Uri.EscapeDataString(orcamento.TokenAcaoExterna);
        return (
            cliente.Contato.Email,
            $"{baseUrl}/api/orcamentos/acoes-externas/aprovar?token={token}",
            $"{baseUrl}/api/orcamentos/acoes-externas/recusar?token={token}");
    }
}
