namespace Oficina.Domain.Oficina.ValueObjects;

public class ItemServicoOs
{
    private ItemServicoOs() { } // EF

    public ItemServicoOs(Guid servicoId)
    {
        if (servicoId == Guid.Empty) throw new ArgumentException("Serviço inválido.");
        ServicoId = servicoId;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ServicoId { get; private set; }
}
