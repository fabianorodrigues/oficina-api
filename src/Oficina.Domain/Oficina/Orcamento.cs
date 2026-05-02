using Oficina.Domain.Oficina.Enums;
using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.Oficina;

public class Orcamento : AgregadoRaiz
{
    private readonly List<OrcamentoItemServico> _itensServico = [];
    private readonly List<OrcamentoItemMaterial> _itensMaterial = [];

    private Orcamento() { } // EF

    public Orcamento(Guid ordemServicoId, decimal valorTotal)
    {
        if (ordemServicoId == Guid.Empty) throw new ArgumentException("Ordem de serviço inválida.");

        OrdemServicoId = ordemServicoId;
        ValorTotal = valorTotal;
        Status = StatusOrcamento.AguardandoAprovacao;
        DataCriacao = DateTimeOffset.UtcNow;
    }

    public Guid OrdemServicoId { get; private set; }
    public StatusOrcamento Status { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTimeOffset DataCriacao { get; private set; }
    public string? TokenAcaoExterna { get; private set; }
    public DateTimeOffset? TokenAcaoExternaExpiraEm { get; private set; }

    public IReadOnlyCollection<OrcamentoItemServico> ItensServico => _itensServico;
    public IReadOnlyCollection<OrcamentoItemMaterial> ItensMaterial => _itensMaterial;

    public void DefinirItensServico(IEnumerable<OrcamentoItemServico> itens)
    {
        _itensServico.Clear();
        _itensServico.AddRange(itens);
    }

    public void DefinirItensMaterial(IEnumerable<OrcamentoItemMaterial> itens)
    {
        _itensMaterial.Clear();
        _itensMaterial.AddRange(itens);
    }

    public void Aprovar()
    {
        if (Status != StatusOrcamento.AguardandoAprovacao)
            throw new InvalidOperationException("Orçamento não está aguardando aprovação.");

        Status = StatusOrcamento.Aprovado;
    }

    public void Recusar()
    {
        if (Status != StatusOrcamento.AguardandoAprovacao)
            throw new InvalidOperationException("Orçamento não está aguardando aprovação.");

        Status = StatusOrcamento.Recusado;
    }

    public void DefinirTokenAcaoExterna(string token, DateTimeOffset expiraEm)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token externo inválido.", nameof(token));

        TokenAcaoExterna = token;
        TokenAcaoExternaExpiraEm = expiraEm;
    }
}

public class OrcamentoItemServico
{
    private OrcamentoItemServico() { } // EF

    public OrcamentoItemServico(Guid servicoId, decimal valorMaoDeObra)
    {
        if (servicoId == Guid.Empty) throw new ArgumentException("Serviço inválido.");
        if (valorMaoDeObra < 0) throw new ArgumentOutOfRangeException(nameof(valorMaoDeObra));

        ServicoId = servicoId;
        ValorMaoDeObra = valorMaoDeObra;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ServicoId { get; private set; }
    public decimal ValorMaoDeObra { get; private set; }
}

public class OrcamentoItemMaterial
{
    private OrcamentoItemMaterial() { } // EF

    public OrcamentoItemMaterial(TipoMaterial tipo, Guid materialId, int quantidade, decimal valorUnitario)
    {
        if (materialId == Guid.Empty) throw new ArgumentException("Material inválido.");
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(nameof(quantidade));
        if (valorUnitario < 0) throw new ArgumentOutOfRangeException(nameof(valorUnitario));

        Tipo = tipo;
        MaterialId = materialId;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public TipoMaterial Tipo { get; private set; }
    public Guid MaterialId { get; private set; }
    public int Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
}
