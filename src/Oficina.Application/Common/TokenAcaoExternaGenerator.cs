using System.Security.Cryptography;

namespace Oficina.Application.Common;

public static class TokenAcaoExternaGenerator
{
    public static string Gerar()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
