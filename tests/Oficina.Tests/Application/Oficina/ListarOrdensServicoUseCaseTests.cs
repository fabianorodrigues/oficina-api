using System.Reflection;
using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class ListarOrdensServicoUseCaseTests
{
    [Fact]
    public async Task Executar_DeveExcluirFinalizadasEEntregues()
    {
        var repo = new Mock<IOficinaRepository>();
        var recebida = OrdemServico.CriarRecebida(Guid.NewGuid());
        var emExecucao = CriarOsEmExecucao();
        var finalizada = CriarOsFinalizada();
        var entregue = CriarOsEntregue();

        repo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recebida, emExecucao, finalizada, entregue]);

        var useCase = new ListarOrdensServicoUseCase(repo.Object);

        var resultado = await useCase.Executar(CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.DoesNotContain(resultado, x => x.Status == StatusOrdemServico.Finalizada.ToString());
        Assert.DoesNotContain(resultado, x => x.Status == StatusOrdemServico.Entregue.ToString());
    }

    [Fact]
    public async Task Executar_DeveOrdenarPorPrioridadeDeStatus()
    {
        var repo = new Mock<IOficinaRepository>();
        var recebida = OrdemServico.CriarRecebida(Guid.NewGuid());
        var aguardando = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var diagnostico = OrdemServico.CriarCorretiva(Guid.NewGuid());
        var emExecucao = CriarOsEmExecucao();

        repo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recebida, aguardando, diagnostico, emExecucao]);

        var useCase = new ListarOrdensServicoUseCase(repo.Object);

        var resultado = await useCase.Executar(CancellationToken.None);

        Assert.Collection(resultado,
            os => Assert.Equal(StatusOrdemServico.EmExecucao.ToString(), os.Status),
            os => Assert.Equal(StatusOrdemServico.AguardandoAprovacao.ToString(), os.Status),
            os => Assert.Equal(StatusOrdemServico.EmDiagnostico.ToString(), os.Status),
            os => Assert.Equal(StatusOrdemServico.Recebida.ToString(), os.Status));
    }

    [Fact]
    public async Task Executar_DeveOrdenarPorDataCriacaoAscDentroDaMesmaPrioridade()
    {
        var repo = new Mock<IOficinaRepository>();
        var maisNova = OrdemServico.CriarRecebida(Guid.NewGuid());
        var maisAntiga = OrdemServico.CriarRecebida(Guid.NewGuid());

        DefinirDataCriacao(maisNova, new DateTimeOffset(2026, 3, 21, 12, 0, 0, TimeSpan.Zero));
        DefinirDataCriacao(maisAntiga, new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero));

        repo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>()))
            .ReturnsAsync([maisNova, maisAntiga]);

        var useCase = new ListarOrdensServicoUseCase(repo.Object);

        var resultado = await useCase.Executar(CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(maisAntiga.Id, resultado[0].Id);
        Assert.Equal(maisNova.Id, resultado[1].Id);
    }

    [Fact]
    public async Task Executar_QuandoNaoHaOrdens_DeveRetornarListaVazia()
    {
        var repo = new Mock<IOficinaRepository>();
        repo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var useCase = new ListarOrdensServicoUseCase(repo.Object);

        var resultado = await useCase.Executar(CancellationToken.None);

        Assert.Empty(resultado);
    }

    private static OrdemServico CriarOsEmExecucao()
    {
        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 100);
        os.VincularOrcamento(orcamento.Id);
        orcamento.Aprovar();
        os.IniciarExecucao(orcamento);
        return os;
    }

    private static OrdemServico CriarOsFinalizada()
    {
        var os = CriarOsEmExecucao();
        os.Finalizar();
        return os;
    }

    private static OrdemServico CriarOsEntregue()
    {
        var os = CriarOsFinalizada();
        os.MarcarEntregue();
        return os;
    }

    private static void DefinirDataCriacao(OrdemServico os, DateTimeOffset dataCriacao)
    {
        var property = typeof(OrdemServico).GetProperty("DataCriacao", BindingFlags.Instance | BindingFlags.Public);
        property!.SetValue(os, dataCriacao);
    }
}
