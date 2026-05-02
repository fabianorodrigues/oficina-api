namespace Oficina.Domain.Oficina.ValueObjects;

public class Diagnostico
{
    private Diagnostico() { } // EF

    public Diagnostico(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição do diagnóstico é obrigatória.");

        Descricao = descricao.Trim();
        DataRegistro = DateTimeOffset.UtcNow;
    }

    public string Descricao { get; private set; } = default!;
    public DateTimeOffset DataRegistro { get; private set; }
}
