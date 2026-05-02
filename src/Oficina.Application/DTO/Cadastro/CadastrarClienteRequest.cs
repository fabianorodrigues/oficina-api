namespace Oficina.Application.DTO.Cadastro;

public record CadastrarClienteRequest(string CpfCnpj, string Nome, string Email, string Telefone);
