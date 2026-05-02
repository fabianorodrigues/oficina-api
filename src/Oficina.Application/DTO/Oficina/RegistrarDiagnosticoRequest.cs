namespace Oficina.Application.DTO.Oficina;

public record RegistrarDiagnosticoRequest(string Descricao, IReadOnlyList<Guid> ServicoIds);
