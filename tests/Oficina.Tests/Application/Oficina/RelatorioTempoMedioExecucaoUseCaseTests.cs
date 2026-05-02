using System.Reflection;
using Moq;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class RelatorioTempoMedioExecucaoUseCaseTests
{
    [Fact]
    public async Task Executar_DeveRetornarNulo_QuandoNaoHaExecucoesCompletas()
    {
        var repo = new Mock<IOficinaRepository>();
        var os = OrdemServico.CriarRecebida(Guid.NewGuid());

        repo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>())).ReturnsAsync([os]);

        var useCase = new RelatorioTempoMedioExecucaoUseCase(repo.Object);
        var resultado = await useCase.Executar(CancellationToken.None);

        Assert.Null(resultado.TempoMedio);
    }

    [Fact]
    public async Task Executar_DeveRetornarMediaDeTempo_QuandoHaOrdensFinalizadas()
    {
        var repo = new Mock<IOficinaRepository>();

        var primeira = OrdemServico.CriarRecebida(Guid.NewGuid());
        var segunda = OrdemServico.CriarRecebida(Guid.NewGuid());

        DefinirIntervaloExecucao(primeira, TimeSpan.FromHours(2));
        DefinirIntervaloExecucao(segunda, TimeSpan.FromHours(4));

        repo.Setup(x => x.ListarOrdensServico(It.IsAny<CancellationToken>())).ReturnsAsync([primeira, segunda]);

        var useCase = new RelatorioTempoMedioExecucaoUseCase(repo.Object);
        var resultado = await useCase.Executar(CancellationToken.None);

        Assert.NotNull(resultado.TempoMedio);
        Assert.Equal(3, resultado.TempoMedio!.Horas);
        Assert.Equal(0, resultado.TempoMedio.Minutos);
    }

    private static void DefinirIntervaloExecucao(OrdemServico os, TimeSpan duracao)
    {
        var inicio = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
        var fim = inicio.Add(duracao);

        typeof(OrdemServico).GetProperty("DataInicioExecucao", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(os, inicio);
        typeof(OrdemServico).GetProperty("DataFimExecucao", BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(os, fim);
    }
}
