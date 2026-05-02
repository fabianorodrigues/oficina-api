using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Domain;

public class OrcamentoTests
{
    [Fact]
    public void Orcamento_DeveIniciarAguardandoAprovacao()
    {
        var o = new Orcamento(Guid.NewGuid(), 10);
        Assert.Equal(StatusOrcamento.AguardandoAprovacao, o.Status);
    }

    [Fact]
    public void Aprovar_DeveMudarStatus()
    {
        var o = new Orcamento(Guid.NewGuid(), 10);
        o.Aprovar();
        Assert.Equal(StatusOrcamento.Aprovado, o.Status);
    }

    [Fact]
    public void Recusar_DeveMudarStatus()
    {
        var o = new Orcamento(Guid.NewGuid(), 10);
        o.Recusar();
        Assert.Equal(StatusOrcamento.Recusado, o.Status);
    }

    [Fact]
    public void NaoPodeAprovarDuasVezes()
    {
        var o = new Orcamento(Guid.NewGuid(), 10);
        o.Aprovar();
        Assert.Throws<InvalidOperationException>(() => o.Aprovar());
    }
}
