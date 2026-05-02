namespace Oficina.Application.DTO.Cadastro;

public record AtualizarClienteRequest(string CpfCnpj, string Nome, string Email, string Telefone);
