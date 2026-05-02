using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Domain;

public class OrdemServicoTests
{
    [Fact]
    public void CriarRecebida_DeveIniciarSemClassificacao()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        Assert.Equal(StatusOrdemServico.Recebida, os.Status);
        Assert.Equal(TipoManutencao.NaoClassificada, os.TipoManutencao);
        Assert.Equal(OrigemAtualizacaoStatusOs.Interna, os.OrigemUltimaAtualizacaoStatus);
    }

    [Fact]
    public void ClassificarPreventiva_DeveMoverParaAguardandoAprovacao()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        os.Classificar(TipoManutencao.Preventiva);

        Assert.Equal(TipoManutencao.Preventiva, os.TipoManutencao);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public void ClassificarCorretiva_DeveMoverParaDiagnostico()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        os.Classificar(TipoManutencao.Corretiva);

        Assert.Equal(TipoManutencao.Corretiva, os.TipoManutencao);
        Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);
    }

    [Fact]
    public void RegistrarDiagnostico_DeveMoverParaAguardandoAprovacao()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        os.Classificar(TipoManutencao.Corretiva);
        os.RegistrarDiagnostico("Problema", new[] { Guid.NewGuid() });

        Assert.NotNull(os.Diagnostico);
        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
    }

    [Fact]
    public void IniciarExecucao_SoComOrcamentoAprovado()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        os.Classificar(TipoManutencao.Preventiva, [Guid.NewGuid()]);

        var orc = new Orcamento(os.Id, 10);
        os.VincularOrcamento(orc.Id);

        Assert.Throws<InvalidOperationException>(() => os.IniciarExecucao(orc));

        orc.Aprovar();
        os.IniciarExecucao(orc);

        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
        Assert.NotNull(os.DataInicioExecucao);
    }

    [Fact]
    public void NaoPermiteReclassificar()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        os.Classificar(TipoManutencao.Corretiva);

        Assert.Throws<InvalidOperationException>(() => os.Classificar(TipoManutencao.Preventiva));
    }
}
