using Oficina.Domain.Cadastro;
using Oficina.Domain.Seguranca;

namespace Oficina.Application.Abstractions.Seguranca;

public interface IJwtTokenService
{
    string GerarTokenCliente(Cliente cliente);
    string GerarTokenFuncionario(Funcionario funcionario);
}
