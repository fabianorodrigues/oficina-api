namespace Oficina.Application.Shared;

public class OficinaException : Exception
{
    public int StatusHttp { get; }

    public OficinaException(string mensagem, int statusHttp = 400) : base(mensagem)
    {
        StatusHttp = statusHttp;
    }
}
