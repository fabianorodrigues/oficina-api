using Oficina.Application.Abstractions.Repositorios;
using Oficina.Application.DTO.Oficina;
using Oficina.Domain.Oficina.Enums;

namespace Oficina.Application.UseCases.Oficina;

public class ProcessarAcaoExternaOrcamentoUseCase
{
    private readonly IOficinaRepository _oficina;
    private readonly AprovarOrcamentoUseCase _aprovarOrcamento;
    private readonly RecusarOrcamentoUseCase _recusarOrcamento;

    public ProcessarAcaoExternaOrcamentoUseCase(
        IOficinaRepository oficina,
        AprovarOrcamentoUseCase aprovarOrcamento,
        RecusarOrcamentoUseCase recusarOrcamento)
    {
        _oficina = oficina;
        _aprovarOrcamento = aprovarOrcamento;
        _recusarOrcamento = recusarOrcamento;
    }

    public async Task<ProcessarAcaoExternaOrcamentoResponse> Executar(ProcessarAcaoExternaOrcamentoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new ProcessarAcaoExternaOrcamentoResponse
            {
                Sucesso = false,
                Codigo = "token_invalido",
                Mensagem = "Token inválido."
            };
        }

        var orcamento = await _oficina.ObterOrcamentoPorTokenAcaoExterna(request.Token, ct);
        if (orcamento is null)
        {
            return new ProcessarAcaoExternaOrcamentoResponse
            {
                Sucesso = false,
                Codigo = "link_invalido",
                Mensagem = "Link inválido."
            };
        }

        if (orcamento.TokenAcaoExternaExpiraEm is not null && orcamento.TokenAcaoExternaExpiraEm < DateTimeOffset.UtcNow)
        {
            return new ProcessarAcaoExternaOrcamentoResponse
            {
                Sucesso = false,
                Codigo = "link_expirado",
                Mensagem = "Este link expirou."
            };
        }

        if (orcamento.Status != StatusOrcamento.AguardandoAprovacao)
        {
            return new ProcessarAcaoExternaOrcamentoResponse
            {
                Sucesso = false,
                Codigo = "acao_ja_processada",
                Mensagem = "Este orçamento já foi processado.",
                OrcamentoId = orcamento.Id,
                OrdemServicoId = orcamento.OrdemServicoId
            };
        }

        if (request.Acao == AcaoExternaOrcamento.Aprovar)
        {
            await _aprovarOrcamento.Executar(orcamento.Id, ct, OrigemAtualizacaoStatusOs.Externa);
            return new ProcessarAcaoExternaOrcamentoResponse
            {
                Sucesso = true,
                Codigo = "aprovado",
                Mensagem = "Orçamento aprovado com sucesso.",
                OrcamentoId = orcamento.Id,
                OrdemServicoId = orcamento.OrdemServicoId
            };
        }

        await _recusarOrcamento.Executar(orcamento.Id, ct, OrigemAtualizacaoStatusOs.Externa);
        return new ProcessarAcaoExternaOrcamentoResponse
        {
            Sucesso = true,
            Codigo = "recusado",
            Mensagem = "Orçamento recusado com sucesso.",
            OrcamentoId = orcamento.Id,
            OrdemServicoId = orcamento.OrdemServicoId
        };
    }
}
