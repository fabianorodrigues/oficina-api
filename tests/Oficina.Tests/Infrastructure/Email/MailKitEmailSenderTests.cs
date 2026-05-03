using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Email;
using Oficina.Infrastructure.Email.Configurations;
using Oficina.Infrastructure.Email.Providers;
using Xunit;

namespace Oficina.Tests.Infrastructure.Email;

public class MailKitEmailSenderTests
{
    [Fact]
    public async Task Enviar_DeveFalharComMensagemControlada_QuandoDestinatarioForInvalido()
    {
        var sender = CriarSender("no-reply@oficina.local");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sender.Enviar(new EmailMessage
        {
            To = "joao.@email.com",
            Subject = "Teste",
            HtmlBody = "<p>Teste</p>"
        }, CancellationToken.None));

        Assert.Contains("destinatario", ex.Message);
        Assert.Contains("joao.@email.com", ex.Message);
    }

    [Fact]
    public async Task Enviar_DeveFalharComMensagemControlada_QuandoRemetenteForInvalido()
    {
        var sender = CriarSender("no-reply.@oficina.local");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sender.Enviar(new EmailMessage
        {
            To = "joao@email.com",
            Subject = "Teste",
            HtmlBody = "<p>Teste</p>"
        }, CancellationToken.None));

        Assert.Contains("remetente", ex.Message);
        Assert.Contains("no-reply.@oficina.local", ex.Message);
    }

    [Fact]
    public async Task Enviar_DeveFalharComMensagemSegura_QuandoAutenticacaoSmtpEstiverParcial()
    {
        var sender = new MailKitEmailSender(
            Options.Create(new EmailSettings
            {
                From = "no-reply@oficina.local",
                SmtpHost = "localhost",
                SmtpPort = 25,
                Username = "usuario"
            }),
            NullLogger<MailKitEmailSender>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.Enviar(new EmailMessage
        {
            To = "joao@email.com",
            Subject = "Teste",
            HtmlBody = "<p>Teste</p>"
        }, CancellationToken.None));

        Assert.Contains("usuario e senha devem ser informados juntos", ex.Message);
        Assert.DoesNotContain("usuario", ex.Message.Replace("usuario e senha", string.Empty));
    }

    private static MailKitEmailSender CriarSender(string from)
        => new(
            Options.Create(new EmailSettings
            {
                From = from,
                SmtpHost = "localhost",
                SmtpPort = 25
            }),
            NullLogger<MailKitEmailSender>.Instance);
}
