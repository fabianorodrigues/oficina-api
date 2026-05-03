using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Oficina.Application.Abstractions.Email;
using Oficina.Infrastructure.Email.Configurations;

namespace Oficina.Infrastructure.Email.Providers;

public class MailKitEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<EmailSettings> settings, ILogger<MailKitEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Enviar(EmailMessage message, CancellationToken ct)
    {
        ValidarAutenticacaoSmtp();

        var email = new MimeMessage();
        email.From.Add(ValidarEndereco(_settings.From, "remetente"));
        email.To.Add(ValidarEndereco(message.To, "destinatario"));
        email.Subject = message.Subject;
        email.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions, ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        }

        await client.SendAsync(email, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("E-mail enviado com sucesso.");
    }

    private void ValidarAutenticacaoSmtp()
    {
        var temUsuario = !string.IsNullOrWhiteSpace(_settings.Username);
        var temSenha = !string.IsNullOrWhiteSpace(_settings.Password);

        if (temUsuario != temSenha)
        {
            throw new InvalidOperationException("Configuracao SMTP invalida: usuario e senha devem ser informados juntos.");
        }
    }

    private MailboxAddress ValidarEndereco(string endereco, string contexto)
    {
        if (MailboxAddress.TryParse(endereco, out var mailbox))
            return mailbox;

        _logger.LogWarning("Endereco de e-mail invalido para {Contexto}: {Email}.", contexto, endereco);
        throw new ArgumentException($"Endereco de e-mail invalido para {contexto}: {endereco}", nameof(endereco));
    }
}
