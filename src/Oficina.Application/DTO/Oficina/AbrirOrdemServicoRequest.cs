namespace Oficina.Application.DTO.Oficina;

public sealed class AbrirOrdemServicoRequest
{
    public ClienteAberturaRequest Cliente { get; init; } = new();
    public VeiculoAberturaRequest Veiculo { get; init; } = new();
    public ItensAberturaRequest Itens { get; init; } = new();
}

public sealed class ClienteAberturaRequest
{
    public string Nome { get; init; } = string.Empty;
    public string Documento { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
}

public sealed class VeiculoAberturaRequest
{
    public string Placa { get; init; } = string.Empty;
    public string Renavam { get; init; } = string.Empty;
    public ModeloAberturaRequest Modelo { get; init; } = new();
}

public sealed class ModeloAberturaRequest
{
    public string Descricao { get; init; } = string.Empty;
    public string Marca { get; init; } = string.Empty;
    public int Ano { get; init; }
}

public sealed class ItensAberturaRequest
{
    public IReadOnlyList<ServicoAberturaRequest> Servicos { get; init; } = [];
    public IReadOnlyList<PecaAberturaRequest> Pecas { get; init; } = [];
    public IReadOnlyList<InsumoAberturaRequest> Insumos { get; init; } = [];
}

public sealed class ServicoAberturaRequest
{
    public Guid ServicoId { get; init; }
}

public sealed class PecaAberturaRequest
{
    public Guid PecaId { get; init; }
    public int Quantidade { get; init; }
}

public sealed class InsumoAberturaRequest
{
    public Guid InsumoId { get; init; }
    public int Quantidade { get; init; }
}
