using Oficina.Domain.Cadastro;
using Oficina.Domain.Cadastro.ValueObjects;
using Oficina.Domain.CatalogoEstoque;
using Oficina.Domain.Oficina;

namespace Oficina.Tests.TestesHelpers;

public static class DomainBuilder
{
    public static Cliente ClienteValido(
        string cpfCnpj = "12345678909",
        string nome = "Cliente Teste",
        string email = "cliente@teste.com",
        string telefone = "11999999999")
        => new Cliente(new DocumentoCpfCnpj(cpfCnpj), nome, new Contato(email, telefone));

    public static Veiculo VeiculoValido(Guid? clienteId = null)
        => new Veiculo(
            clienteId ?? Guid.NewGuid(),
            new Placa("ABC1D23"),
            new Renavam("12345678901"),
            new Modelo("Civic", "Honda", 2020));

    public static Servico ServicoSomenteMaoDeObra(decimal maoDeObra = 100m)
        => new Servico(maoDeObra);

    public static Servico ServicoComMateriais(Guid pecaId, Guid insumoId)
    {
        var s = new Servico(200m);
        s.AdicionarPeca(pecaId, 2);
        s.AdicionarInsumo(insumoId, 1);
        return s;
    }

    public static Peca PecaValida(decimal preco = 10m)
        => new Peca(preco, "Filtro de óleo");

    public static Insumo InsumoValido(decimal preco = 5m)
        => new Insumo(preco, "Óleo 5W30");

    public static EstoquePeca EstoquePeca(Guid pecaId, int qtd)
        => new EstoquePeca(pecaId, qtd);

    public static EstoqueInsumo EstoqueInsumo(Guid insumoId, int qtd)
        => new EstoqueInsumo(insumoId, qtd);

    public static OrdemServico OsPreventiva(Guid veiculoId, params Guid[] servicos)
        => OrdemServico.CriarPreventiva(veiculoId, servicos);

    public static OrdemServico OsCorretiva(Guid veiculoId)
        => OrdemServico.CriarCorretiva(veiculoId);
}
