using Oficina.Application.Common;
using Xunit;

namespace Oficina.Tests.Application;

public class TokenAcaoExternaGeneratorTests
{
    [Fact]
    public void Gerar_DeveRetornarHexCom64Caracteres()
    {
        var token = TokenAcaoExternaGenerator.Gerar();

        Assert.Equal(64, token.Length);
        Assert.Matches("^[0-9A-F]+$", token);
    }

    [Fact]
    public void Gerar_DeveRetornarTokensDiferentes()
    {
        var token1 = TokenAcaoExternaGenerator.Gerar();
        var token2 = TokenAcaoExternaGenerator.Gerar();

        Assert.NotEqual(token1, token2);
    }
}
