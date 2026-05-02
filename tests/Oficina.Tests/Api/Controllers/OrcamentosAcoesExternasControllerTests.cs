using Microsoft.AspNetCore.Mvc;
using Moq;
using Oficina.Application.DTO.Oficina;
using Oficina.Api.Controllers;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Api.Controllers;

public class OrcamentosAcoesExternasControllerTests
{
    [Fact]
    public async Task Aprovar_ComTokenEmBranco_DeveRetornarBadRequest()
    {
        var controller = new OrcamentosAcoesExternasController(CriarUseCase(new Mock<IOficinaRepository>()));

        var result = await controller.Aprovar(" ", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<ProcessarAcaoExternaOrcamentoResponse>(badRequest.Value);
        Assert.Equal("token_invalido", payload.Codigo);
    }

    [Fact]
    public async Task Aprovar_ComTokenInvalido_DeveRetornarNotFound()
    {
        var repo = new Mock<IOficinaRepository>();
        repo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKEN_INEXISTENTE", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Orcamento?)null);

        var controller = new OrcamentosAcoesExternasController(CriarUseCase(repo));

        var result = await controller.Aprovar("TOKEN_INEXISTENTE", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var payload = Assert.IsType<ProcessarAcaoExternaOrcamentoResponse>(notFound.Value);
        Assert.Equal("link_invalido", payload.Codigo);
    }

    [Fact]
    public async Task Aprovar_ComTokenValido_DeveRetornarOkComRespostaDaApplication()
    {
        var repo = new Mock<IOficinaRepository>();
        var estoque = new Mock<ICatalogoEstoqueRepository>();

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 100);
        orcamento.DefinirTokenAcaoExterna("TOKEN_OK", DateTimeOffset.UtcNow.AddHours(1));
        os.VincularOrcamento(orcamento.Id);

        repo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKEN_OK", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        repo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        repo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(
            repo.Object,
            new AprovarOrcamentoUseCase(repo.Object, estoque.Object),
            new RecusarOrcamentoUseCase(repo.Object, Mock.Of<INotificadorCliente>()));

        var controller = new OrcamentosAcoesExternasController(useCase);

        var result = await controller.Aprovar("TOKEN_OK", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ProcessarAcaoExternaOrcamentoResponse>(ok.Value);
        Assert.True(payload.Sucesso);
        Assert.Equal("aprovado", payload.Codigo);
    }

    private static ProcessarAcaoExternaOrcamentoUseCase CriarUseCase(Mock<IOficinaRepository> repo)
        => new(
            repo.Object,
            new AprovarOrcamentoUseCase(repo.Object, new Mock<ICatalogoEstoqueRepository>().Object),
            new RecusarOrcamentoUseCase(repo.Object, new Mock<INotificadorCliente>().Object));
}
