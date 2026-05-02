using Oficina.Domain.Cadastro.ValueObjects;
using Xunit;

namespace Oficina.Tests.Domain.Cadastro;

public class PlacaRenavamModeloContatoTests
{
    [Theory]
    [InlineData("abc1d23", "ABC1D23")]
    [InlineData("ABC1D23", "ABC1D23")]
    [InlineData("KLA-1729", "KLA1729")]
    public void Placa_deve_normalizar_para_maiusculo(string placa, string esperado)
        => Assert.Equal(esperado, new Placa(placa).Valor);

    [Theory]
    [InlineData("")]
    [InlineData("AAAAAAA")]
    [InlineData("12132113")]
    
    public void Placa_deve_lancar_quando_formato_invalido(string placa)
        => Assert.Throws<ArgumentException>(() => new Placa(placa));

    [Theory]
    [InlineData("12345678901")]
    [InlineData(" 12345678901 ")]
    public void Renavam_deve_aceitar_11_digitos_e_trim(string renavam)
        => Assert.Equal("12345678901", new Renavam(renavam).Valor);

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890A")]
    public void Renavam_deve_lancar_quando_invalido(string renavam)
        => Assert.Throws<ArgumentException>(() => new Renavam(renavam));

    [Fact]
    public void Modelo_deve_lancar_quando_ano_invalido()
        => Assert.Throws<ArgumentException>(() => new Modelo("Civic", "Honda", 1800));

    [Fact]
    public void Contato_deve_lancar_quando_email_ou_telefone_vazio()
    {
        Assert.Throws<ArgumentException>(() => new Contato("", "119"));
        Assert.Throws<ArgumentException>(() => new Contato("a@a.com", ""));
    }
}
