using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using UnitTests.Support;

namespace UnitTests.Domain;

public class PedidoTests
{
    [Fact]
    public void AdicionarItem_ProdutoComPreco_CapturaPrecoEDebitaEstoque()
    {
        var produto = ProdutoBuilder.Criar(1, "Teclado", 199.90m, 5);
        var pedido = new Pedido(clienteId: 10, dataPedido: new DateTime(2026, 6, 1, 12, 0, 0));

        var item = pedido.AdicionarItem(produto, 2);

        item.ProdutoId.Should().Be(1);
        item.Quantidade.Should().Be(2);
        item.PrecoUnitario.Should().Be(199.90m);
        item.Subtotal.Should().Be(399.80m);
        pedido.ValorTotal.Should().Be(399.80m);
        produto.Estoque.Should().Be(3);
    }

    [Fact]
    public void AdicionarItem_VariosItens_RecalculaTotal()
    {
        var notebook = ProdutoBuilder.Criar(1, "Notebook", 3000m, 10);
        var mouse = ProdutoBuilder.Criar(2, "Mouse", 50.25m, 10);
        var pedido = new Pedido(1, DateTime.UtcNow);

        pedido.AdicionarItem(notebook, 1);
        pedido.AdicionarItem(mouse, 2);

        pedido.ValorTotal.Should().Be(3100.50m);
        pedido.Itens.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AdicionarItem_QuantidadeInvalida_LancaQuantidadeInvalidaException(int quantidade)
    {
        var produto = ProdutoBuilder.Criar(1, "Mouse", 50m, 10);
        var pedido = new Pedido(1, DateTime.UtcNow);

        var act = () => pedido.AdicionarItem(produto, quantidade);

        act.Should().Throw<QuantidadeInvalidaException>();
        produto.Estoque.Should().Be(10);
        pedido.Itens.Should().BeEmpty();
    }

    [Fact]
    public void Validar_SemItens_LancaPedidoSemItensException()
    {
        var pedido = new Pedido(1, DateTime.UtcNow);

        var act = () => pedido.Validar();

        act.Should().Throw<PedidoSemItensException>();
    }

    [Fact]
    public void Validar_ComItens_NaoLanca()
    {
        var produto = ProdutoBuilder.Criar(1, "Mouse", 50m, 10);
        var pedido = new Pedido(1, DateTime.UtcNow);
        pedido.AdicionarItem(produto, 1);

        var act = () => pedido.Validar();

        act.Should().NotThrow();
    }
}
