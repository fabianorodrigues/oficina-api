using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Domain;

public class OrcamentoRegrasTests
{
    [Fact]
    public void Recusar_DeveFalharQuandoJaAprovado()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), 100);
        orcamento.Aprovar();

        Assert.Throws<InvalidOperationException>(() => orcamento.Recusar());
    }

    [Fact]
    public void DefinirTokenAcaoExterna_DeveFalharComTokenVazio()
    {
        var orcamento = new Orcamento(Guid.NewGuid(), 100);

        Assert.Throws<ArgumentException>(() =>
            orcamento.DefinirTokenAcaoExterna(" ", DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public void OrcamentoItemMaterial_DeveValidarParametros()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrcamentoItemMaterial(TipoMaterial.Peca, Guid.Empty, 1, 10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrcamentoItemMaterial(TipoMaterial.Insumo, Guid.NewGuid(), 0, 10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrcamentoItemMaterial(TipoMaterial.Insumo, Guid.NewGuid(), 1, -1));
    }

    [Fact]
    public void OrcamentoItemServico_DeveFalharComParametrosInvalidos()
    {
        Assert.Throws<ArgumentException>(() => new OrcamentoItemServico(Guid.Empty, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrcamentoItemServico(Guid.NewGuid(), -1));
    }
}
