using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Domain;

public class OrdemServicoStatusTransitionsTests
{
    [Fact]
    public void ClassificarPreventiva_ComOrigemExterna_DevePersistirOrigem()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        os.Classificar(TipoManutencao.Preventiva, [Guid.NewGuid()], OrigemAtualizacaoStatusOs.Externa);

        Assert.Equal(StatusOrdemServico.AguardandoAprovacao, os.Status);
        Assert.Equal(OrigemAtualizacaoStatusOs.Externa, os.OrigemUltimaAtualizacaoStatus);
    }

    [Fact]
    public void RegistrarDiagnostico_DeveFalharSemServicoIdentificado()
    {
        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            os.RegistrarDiagnostico("Falha intermitente", []));
    }

    [Fact]
    public void FinalizarPorRecusaOrcamento_DeveFalharQuandoOrcamentoNaoRecusado()
    {
        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 100);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            os.FinalizarPorRecusaOrcamento(orcamento));

        Assert.Contains("recusa", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalizarPorRecusaOrcamento_DeveFalharQuandoOrcamentoDeOutraOs()
    {
        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(Guid.NewGuid(), 120);
        orcamento.Recusar();

        Assert.Throws<InvalidOperationException>(() =>
            os.FinalizarPorRecusaOrcamento(orcamento));
    }

    [Fact]
    public void IniciarDiagnostico_DeveFalharParaOsPreventiva()
    {
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());
        os.Classificar(TipoManutencao.Preventiva, [Guid.NewGuid()]);

        Assert.Throws<InvalidOperationException>(() => os.IniciarDiagnostico());
    }
}
