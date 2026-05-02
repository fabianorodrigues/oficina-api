using Oficina.Domain.Cadastro.ValueObjects;
using Xunit;

namespace Oficina.Tests.Domain;

public class CadastroValueObjectsTests
{
    [Fact]
    public void DocumentoCpfCnpj_DeveNormalizarParaApenasDigitos()
    {
        var doc = new DocumentoCpfCnpj("11.444.777/0001-61");
        Assert.Equal("11444777000161", doc.Valor);
    }

    [Fact]
    public void DocumentoCpfCnpj_Invalido_DeveFalhar()
    {
        Assert.Throws<ArgumentException>(() => new DocumentoCpfCnpj("123"));
    }

    [Fact]
    public void DocumentoCpfCnpj_CpfComDigitoInvalido_DeveFalhar()
    {
        Assert.Throws<ArgumentException>(() => new DocumentoCpfCnpj("123.456.789-00"));
    }

    [Fact]
    public void Placa_DeveNormalizar()
    {
        var placa = new Placa("abc-1234");
        Assert.Equal("ABC1234", placa.Valor);
    }

    [Fact]
    public void Renavam_DeveNormalizar()
    {
        var renavam = new Renavam("12.345.678-901");
        Assert.Equal("12345678901", renavam.Valor);
    }
}
