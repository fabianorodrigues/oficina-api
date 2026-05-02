namespace Oficina.Application.Abstractions.Email;

public class EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
}
