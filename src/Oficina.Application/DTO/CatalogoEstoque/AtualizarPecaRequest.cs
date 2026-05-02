namespace Oficina.Application.DTO.CatalogoEstoque;

public record AtualizarPecaRequest(
    decimal PrecoUnitario,
    string Descricao
);
