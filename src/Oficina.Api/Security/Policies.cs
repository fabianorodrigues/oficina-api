namespace Oficina.Api.Security;

public static class Policies
{
    public const string ClienteOnly = "ClienteOnly";
    public const string FuncionarioOuAdmin = "FuncionarioOuAdmin";
    public const string AdminOnly = "AdminOnly";
    public const string ClienteOuAdmin = "ClienteOuAdmin";
}
