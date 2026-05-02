using Oficina.Domain.Cadastro.ValueObjects;
using Xunit;

namespace Oficina.Tests.Domain.Cadastro;

public class DocumentoCpfCnpjTests
{
    [Theory]
    [InlineData("123.456.789-09", "12345678909")]
    [InlineData("11.444.777/0001-61", "11444777000161")]
    [InlineData("cpf: 123.456.789-09 #ok", "12345678909")]
    [InlineData("cnpj=11.444.777/0001-61", "11444777000161")]
    public void Deve_normalizar_documento_removendo_caracteres(string input, string esperado)
    {
        var doc = new DocumentoCpfCnpj(input);
        Assert.Equal(esperado, doc.Valor);
    }

    [Theory]
    [InlineData("123.456.789-09")]
    [InlineData("529.982.247-25")]
    [InlineData("11444777000161")]
    [InlineData("04.252.011/0001-10")]
    public void Deve_aceitar_documentos_validos(string input)
        => Assert.Equal(new string(input.Where(char.IsDigit).ToArray()), new DocumentoCpfCnpj(input).Valor);

    [Theory]
    [InlineData("123.456.789-00")]
    [InlineData("529.982.247-24")]
    [InlineData("111.111.111-11")]
    [InlineData("11.444.777/0001-62")]
    [InlineData("04.252.011/0001-11")]
    [InlineData("00.000.000/0000-00")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("12.34.56/78xx0001--61z9")] // quantidade de dígitos inválida após normalização
    public void Deve_lancar_quando_documento_invalido(string input)
        => Assert.Throws<ArgumentException>(() => new DocumentoCpfCnpj(input));
}
