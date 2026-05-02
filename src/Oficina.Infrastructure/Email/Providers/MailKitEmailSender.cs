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
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_settings.From));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;
        email.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions, ct);
        await client.SendAsync(email, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("E-mail enviado para {EmailTo} via SMTP {SmtpHost}:{SmtpPort}.", message.To, _settings.SmtpHost, _settings.SmtpPort);
    }
}
