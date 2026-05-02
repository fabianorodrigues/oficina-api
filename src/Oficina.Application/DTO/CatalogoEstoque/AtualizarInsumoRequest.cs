namespace Oficina.Application.DTO.CatalogoEstoque;

public record AtualizarInsumoRequest(
    decimal PrecoUnitario,
    string Descricao
);
