using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.Cadastro;

public class Veiculo : AgregadoRaiz
{
    private Veiculo() { } // EF

    public Veiculo(Guid clienteId, Placa placa, Renavam renavam, Modelo modelo)
    {
        if (clienteId == Guid.Empty) throw new ArgumentException("Cliente inválido.");
        ClienteId = clienteId;
        Placa = placa ?? throw new ArgumentNullException(nameof(placa));
        Renavam = renavam ?? throw new ArgumentNullException(nameof(renavam));
        Modelo = modelo ?? throw new ArgumentNullException(nameof(modelo));
    }

    public Guid ClienteId { get; private set; }
    public Placa Placa { get; private set; } = default!;
    public Renavam Renavam { get; private set; } = default!;
    public Modelo Modelo { get; private set; } = default!;

    public void AlterarPlaca(Placa placa) => Placa = placa ?? throw new ArgumentNullException(nameof(placa));
    public void AlterarRenavam(Renavam renavam) => Renavam = renavam ?? throw new ArgumentNullException(nameof(renavam));

    public void AtualizarModelo(Modelo modelo) => Modelo = modelo ?? throw new ArgumentNullException(nameof(modelo));
}
