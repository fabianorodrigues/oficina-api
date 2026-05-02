namespace Oficina.Application.DTO.Cadastro;

public record CadastrarVeiculoRequest(Guid ClienteId, string Placa, string Renavam, ModeloRequest Modelo);
