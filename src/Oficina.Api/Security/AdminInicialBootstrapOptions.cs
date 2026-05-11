namespace Oficina.Api.Security;

public sealed class AdminInicialBootstrapOptions
{
    public int MaxTentativas { get; set; } = 12;

    public int IntervaloSegundos { get; set; } = 5;

    public TimeSpan Intervalo => TimeSpan.FromSeconds(Math.Max(0, IntervaloSegundos));
}
