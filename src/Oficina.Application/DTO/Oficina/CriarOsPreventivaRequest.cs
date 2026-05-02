namespace Oficina.Application.DTO.Oficina;

public record CriarOsPreventivaRequest(Guid VeiculoId, IReadOnlyList<Guid> ServicoIds);
