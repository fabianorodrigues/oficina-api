namespace Oficina.Application.Abstractions.Email;

public interface IEmailSender
{
    Task Enviar(EmailMessage message, CancellationToken ct);
}
