using Moq;
using Oficina.Application.Abstractions.Notificacoes;
using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;
using Oficina.Application.Shared;
using Oficina.Application.UseCases.Oficina;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;
using Oficina.Domain.Oficina.Enums;
using Xunit;

namespace Oficina.Tests.Application.Oficina;

public class ProcessarAcaoExternaOrcamentoUseCaseTests
{
    [Fact]
    public async Task AprovarExterno_DeveAtualizarOrcamentoOsEOrigemExterna()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var estoqueRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var orcamento = new Orcamento(os.Id, 100);
        orcamento.DefinirTokenAcaoExterna("TOKEN123", DateTimeOffset.UtcNow.AddHours(2));
        os.VincularOrcamento(orcamento.Id);

        oficinaRepo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKEN123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var aprovar = new AprovarOrcamentoUseCase(oficinaRepo.Object, estoqueRepo.Object);
        var recusar = new RecusarOrcamentoUseCase(oficinaRepo.Object, notificador.Object);
        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(oficinaRepo.Object, aprovar, recusar);

        var resposta = await useCase.Executar(new ProcessarAcaoExternaOrcamentoRequest
        {
            Token = "TOKEN123",
            Acao = AcaoExternaOrcamento.Aprovar
        }, CancellationToken.None);

        Assert.True(resposta.Sucesso);
        Assert.Equal("aprovado", resposta.Codigo);
        Assert.Equal(StatusOrcamento.Aprovado, orcamento.Status);
        Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);
        Assert.Equal(OrigemAtualizacaoStatusOs.Externa, os.OrigemUltimaAtualizacaoStatus);
    }

    [Fact]
    public async Task RecusarExterno_DeveAtualizarOrcamentoOsEOrigemExterna()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var estoqueRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarCorretiva(Guid.NewGuid());
        os.RegistrarDiagnostico("Falha no motor", [Guid.NewGuid()]);

        var orcamento = new Orcamento(os.Id, 200);
        orcamento.DefinirTokenAcaoExterna("TOKENRECUSAR", DateTimeOffset.UtcNow.AddHours(2));
        os.VincularOrcamento(orcamento.Id);

        oficinaRepo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKENRECUSAR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);

        var aprovar = new AprovarOrcamentoUseCase(oficinaRepo.Object, estoqueRepo.Object);
        var recusar = new RecusarOrcamentoUseCase(oficinaRepo.Object, notificador.Object);
        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(oficinaRepo.Object, aprovar, recusar);

        var resposta = await useCase.Executar(new ProcessarAcaoExternaOrcamentoRequest
        {
            Token = "TOKENRECUSAR",
            Acao = AcaoExternaOrcamento.Recusar
        }, CancellationToken.None);

        Assert.True(resposta.Sucesso);
        Assert.Equal("recusado", resposta.Codigo);
        Assert.Equal(StatusOrcamento.Recusado, orcamento.Status);
        Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
        Assert.Equal(OrigemAtualizacaoStatusOs.Externa, os.OrigemUltimaAtualizacaoStatus);
    }

    [Fact]
    public async Task TokenInvalido_DeveRetornarFalha()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var aprovar = new AprovarOrcamentoUseCase(oficinaRepo.Object, new Mock<ICatalogoEstoqueRepository>().Object);
        var recusar = new RecusarOrcamentoUseCase(oficinaRepo.Object, new Mock<INotificadorCliente>().Object);
        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(oficinaRepo.Object, aprovar, recusar);

        var resposta = await useCase.Executar(new ProcessarAcaoExternaOrcamentoRequest
        {
            Token = "TOKEN_QUE_NAO_EXISTE",
            Acao = AcaoExternaOrcamento.Aprovar
        }, CancellationToken.None);

        Assert.False(resposta.Sucesso);
        Assert.Equal("link_invalido", resposta.Codigo);
    }

    [Fact]
    public async Task OrcamentoInexistenteAoExecutarFluxoInterno_DeveRetornarFalha()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var estoqueRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();
        var orcamento = new Orcamento(Guid.NewGuid(), 100);
        orcamento.DefinirTokenAcaoExterna("TOKENSEMOS", DateTimeOffset.UtcNow.AddHours(2));

        oficinaRepo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKENSEMOS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(orcamento.OrdemServicoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrdemServico?)null);

        var aprovar = new AprovarOrcamentoUseCase(oficinaRepo.Object, estoqueRepo.Object);
        var recusar = new RecusarOrcamentoUseCase(oficinaRepo.Object, notificador.Object);
        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(oficinaRepo.Object, aprovar, recusar);

        await Assert.ThrowsAsync<OficinaException>(() => useCase.Executar(
            new ProcessarAcaoExternaOrcamentoRequest { Token = "TOKENSEMOS", Acao = AcaoExternaOrcamento.Aprovar },
            CancellationToken.None));
    }

    [Fact]
    public async Task LinkExpirado_DeveRetornarFalha()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var estoqueRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();
        var orcamento = new Orcamento(Guid.NewGuid(), 100);
        orcamento.DefinirTokenAcaoExterna("TOKENEXPIRADO", DateTimeOffset.UtcNow.AddMinutes(-1));

        oficinaRepo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKENEXPIRADO", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);

        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(
            oficinaRepo.Object,
            new AprovarOrcamentoUseCase(oficinaRepo.Object, estoqueRepo.Object),
            new RecusarOrcamentoUseCase(oficinaRepo.Object, notificador.Object));

        var resposta = await useCase.Executar(
            new ProcessarAcaoExternaOrcamentoRequest { Token = "TOKENEXPIRADO", Acao = AcaoExternaOrcamento.Recusar },
            CancellationToken.None);

        Assert.False(resposta.Sucesso);
        Assert.Equal("link_expirado", resposta.Codigo);
    }

    [Fact]
    public async Task TokenEmBranco_DeveRetornarTokenInvalido()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(
            oficinaRepo.Object,
            new AprovarOrcamentoUseCase(oficinaRepo.Object, new Mock<ICatalogoEstoqueRepository>().Object),
            new RecusarOrcamentoUseCase(oficinaRepo.Object, new Mock<INotificadorCliente>().Object));

        var resposta = await useCase.Executar(
            new ProcessarAcaoExternaOrcamentoRequest { Token = " ", Acao = AcaoExternaOrcamento.Aprovar },
            CancellationToken.None);

        Assert.False(resposta.Sucesso);
        Assert.Equal("token_invalido", resposta.Codigo);
    }

    [Fact]
    public async Task AprovarExterno_DeveAplicarMesmaRegraInternaDeBaixaDeEstoque()
    {
        var oficinaRepo = new Mock<IOficinaRepository>();
        var estoqueRepo = new Mock<ICatalogoEstoqueRepository>();
        var notificador = new Mock<INotificadorCliente>();

        var os = OrdemServico.CriarPreventiva(Guid.NewGuid(), [Guid.NewGuid()]);
        var pecaId = Guid.NewGuid();
        var orcamento = new Orcamento(os.Id, 150);
        orcamento.DefinirItensMaterial([new OrcamentoItemMaterial(TipoMaterial.Peca, pecaId, 2, 75)]);
        orcamento.DefinirTokenAcaoExterna("TOKEN_ESTOQUE", DateTimeOffset.UtcNow.AddHours(2));
        os.VincularOrcamento(orcamento.Id);

        var estoquePeca = new EstoquePeca(pecaId, 10);

        oficinaRepo.Setup(x => x.ObterOrcamentoPorTokenAcaoExterna("TOKEN_ESTOQUE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrcamento(orcamento.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamento);
        oficinaRepo.Setup(x => x.ObterOrdemServico(os.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(os);
        estoqueRepo.Setup(x => x.ObterEstoquePeca(pecaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estoquePeca);

        var useCase = new ProcessarAcaoExternaOrcamentoUseCase(
            oficinaRepo.Object,
            new AprovarOrcamentoUseCase(oficinaRepo.Object, estoqueRepo.Object),
            new RecusarOrcamentoUseCase(oficinaRepo.Object, notificador.Object));

        var resposta = await useCase.Executar(
            new ProcessarAcaoExternaOrcamentoRequest { Token = "TOKEN_ESTOQUE", Acao = AcaoExternaOrcamento.Aprovar },
            CancellationToken.None);

        Assert.True(resposta.Sucesso);
        Assert.Equal(8, estoquePeca.Quantidade);
        Assert.Equal(OrigemAtualizacaoStatusOs.Externa, os.OrigemUltimaAtualizacaoStatus);
        estoqueRepo.Verify(x => x.Salvar(It.IsAny<CancellationToken>()), Times.Once);
    }
}
