using Oficina.Domain.Oficina.Enums;
using Oficina.Domain.Oficina.ValueObjects;
using Oficina.Domain.SharedKernel;

namespace Oficina.Domain.Oficina;

public class OrdemServico : AgregadoRaiz
{
    private readonly List<ItemServicoOs> _itensServico = [];

    private OrdemServico() { } // EF

    private OrdemServico(Guid veiculoId)
    {
        if (veiculoId == Guid.Empty) throw new ArgumentException("Veículo inválido.");

        VeiculoId = veiculoId;
        TipoManutencao = TipoManutencao.NaoClassificada;
        DataCriacao = DateTimeOffset.UtcNow;

        AtualizarStatus(StatusOrdemServico.Recebida, OrigemAtualizacaoStatusOs.Interna);
    }

    public Guid VeiculoId { get; private set; }
    public TipoManutencao TipoManutencao { get; private set; }
    public StatusOrdemServico Status { get; private set; }
    public OrigemAtualizacaoStatusOs OrigemUltimaAtualizacaoStatus { get; private set; }
    public DateTimeOffset DataUltimaAtualizacaoStatus { get; private set; }

    public DateTimeOffset DataCriacao { get; private set; }
    public DateTimeOffset? DataInicioExecucao { get; private set; }
    public DateTimeOffset? DataFimExecucao { get; private set; }

    public Guid? OrcamentoId { get; private set; }
    public Diagnostico? Diagnostico { get; private set; }

    public IReadOnlyCollection<ItemServicoOs> ItensServico => _itensServico;

    public static OrdemServico CriarRecebida(Guid veiculoId)
        => new(veiculoId);

    public static OrdemServico CriarPreventiva(Guid veiculoId, IEnumerable<Guid> servicoIds)
    {
        var lista = servicoIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (lista.Count == 0) throw new ArgumentException("OS preventiva exige ao menos 1 serviço.");

        var os = new OrdemServico(veiculoId);
        os.Classificar(TipoManutencao.Preventiva, lista);
        return os;
    }

    public static OrdemServico CriarCorretiva(Guid veiculoId)
    {
        var os = new OrdemServico(veiculoId);
        os.Classificar(TipoManutencao.Corretiva);
        return os;
    }

    public void Classificar(
        TipoManutencao tipo,
        IEnumerable<Guid>? servicoIds = null,
        OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (Status != StatusOrdemServico.Recebida)
            throw new InvalidOperationException("Somente OS recebida pode ser classificada.");

        if (TipoManutencao != TipoManutencao.NaoClassificada)
            throw new InvalidOperationException("OS já classificada.");

        if (tipo == TipoManutencao.NaoClassificada)
            throw new InvalidOperationException("Tipo de manutenção inválido para classificação.");

        if (tipo == TipoManutencao.Preventiva)
        {
            var lista = servicoIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
            if (lista.Count > 0)
            {
                _itensServico.Clear();
                foreach (var id in lista) _itensServico.Add(new ItemServicoOs(id));
            }

            TipoManutencao = TipoManutencao.Preventiva;
            AtualizarStatus(StatusOrdemServico.AguardandoAprovacao, origem);
            return;
        }

        TipoManutencao = TipoManutencao.Corretiva;
        AtualizarStatus(StatusOrdemServico.EmDiagnostico, origem);
    }

    public void IniciarDiagnostico(OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (TipoManutencao != TipoManutencao.Corretiva)
            throw new InvalidOperationException("Diagnóstico só existe em OS corretiva.");

        if (Status != StatusOrdemServico.Recebida)
            throw new InvalidOperationException("Diagnóstico só pode iniciar quando a OS estiver recebida.");

        AtualizarStatus(StatusOrdemServico.EmDiagnostico, origem);
    }

    public void RegistrarDiagnostico(string descricao, IEnumerable<Guid> servicoIds, OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (TipoManutencao != TipoManutencao.Corretiva)
            throw new InvalidOperationException("Diagnóstico só existe em OS corretiva.");

        if (Status != StatusOrdemServico.Recebida && Status != StatusOrdemServico.EmDiagnostico)
            throw new InvalidOperationException("OS não está em diagnóstico.");

        Diagnostico = new Diagnostico(descricao);

        var lista = servicoIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (lista.Count == 0) throw new ArgumentException("Diagnóstico exige ao menos 1 serviço identificado.");

        AtualizarStatus(StatusOrdemServico.AguardandoAprovacao, origem);
    }

    public void VincularOrcamento(Guid orcamentoId, bool atualizarStatusParaAguardando = true)
    {
        if (orcamentoId == Guid.Empty) throw new ArgumentException("Orçamento inválido.");

        if (Status is not StatusOrdemServico.Recebida and not StatusOrdemServico.AguardandoAprovacao)
            throw new InvalidOperationException("Orçamento só pode ser vinculado quando a OS estiver recebida ou aguardando aprovação.");

        OrcamentoId = orcamentoId;

        if (Status == StatusOrdemServico.Recebida && atualizarStatusParaAguardando)
            AtualizarStatus(StatusOrdemServico.AguardandoAprovacao, OrigemAtualizacaoStatusOs.Interna);
    }

    public void IniciarExecucao(Orcamento orcamento, OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (orcamento is null) throw new ArgumentNullException(nameof(orcamento));
        if (orcamento.OrdemServicoId != Id)
            throw new InvalidOperationException("Orçamento não corresponde à OS.");

        if (orcamento.Status != StatusOrcamento.Aprovado)
            throw new InvalidOperationException("Execução só pode iniciar após aprovação do orçamento.");

        if (Status != StatusOrdemServico.AguardandoAprovacao)
            throw new InvalidOperationException("OS não está aguardando aprovação.");

        AtualizarStatus(StatusOrdemServico.EmExecucao, origem);
        DataInicioExecucao = DateTimeOffset.UtcNow;
    }

    public void Finalizar(OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (Status != StatusOrdemServico.EmExecucao)
            throw new InvalidOperationException("OS só pode ser finalizada após execução.");

        AtualizarStatus(StatusOrdemServico.Finalizada, origem);
        DataFimExecucao = DateTimeOffset.UtcNow;
    }

    public void MarcarEntregue(OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (Status != StatusOrdemServico.Finalizada)
            throw new InvalidOperationException("OS só pode ser entregue após finalização.");

        AtualizarStatus(StatusOrdemServico.Entregue, origem);
    }

    public void FinalizarPorRecusaOrcamento(Orcamento orcamento, OrigemAtualizacaoStatusOs origem = OrigemAtualizacaoStatusOs.Interna)
    {
        if (orcamento is null) throw new ArgumentNullException(nameof(orcamento));
        if (orcamento.OrdemServicoId != Id)
            throw new InvalidOperationException("Orçamento não corresponde à OS.");

        if (orcamento.Status != StatusOrcamento.Recusado)
            throw new InvalidOperationException("OS só finaliza por recusa quando o orçamento estiver recusado.");

        AtualizarStatus(StatusOrdemServico.Finalizada, origem);
        // sem execução; sem cobrança do diagnóstico (fora do sistema)
    }

    private void AtualizarStatus(StatusOrdemServico novoStatus, OrigemAtualizacaoStatusOs origem)
    {
        Status = novoStatus;
        OrigemUltimaAtualizacaoStatus = origem;
        DataUltimaAtualizacaoStatus = DateTimeOffset.UtcNow;
    }
}
