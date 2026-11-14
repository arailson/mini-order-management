using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using UnitTests.Support;

namespace UnitTests.Domain;

public class ProdutoTests
{
    [Fact]
    public void Construtor_DadosValidos_CriaProduto()
    {
        var produto = new Produto("Notebook", 3500.50m, 10);

        produto.Nome.Should().Be("Notebook");
        produto.Preco.Should().Be(3500.50m);
        produto.Estoque.Should().Be(10);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_NomeInvalido_LancaDomainException(string? nome)
    {
        var act = () => new Produto(nome!, 10m, 1);

        act.Should().Throw<DomainException>()
            .WithMessage("*nome*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_PrecoInvalido_LancaPrecoInvalidoException(decimal preco)
    {
        var act = () => new Produto("Mouse", preco, 1);

        act.Should().Throw<PrecoInvalidoException>();
    }

    [Fact]
    public void Construtor_EstoqueNegativo_LancaEstoqueInvalidoException()
    {
        var act = () => new Produto("Mouse", 10m, -1);

        act.Should().Throw<EstoqueInvalidoException>();
    }

    [Fact]
    public void DebitarEstoque_QuantidadeValida_ReduzEstoque()
    {
        var produto = ProdutoBuilder.Criar(1, "Mouse", 50m, 10);

        produto.DebitarEstoque(3);

        produto.Estoque.Should().Be(7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DebitarEstoque_QuantidadeInvalida_LancaQuantidadeInvalidaException(int quantidade)
    {
        var produto = ProdutoBuilder.Criar(1, "Mouse", 50m, 10);

        var act = () => produto.DebitarEstoque(quantidade);

        act.Should().Throw<QuantidadeInvalidaException>();
        produto.Estoque.Should().Be(10);
    }

    [Fact]
    public void DebitarEstoque_EstoqueInsuficiente_LancaComDados()
    {
        var produto = ProdutoBuilder.Criar(7, "Cabo HDMI", 29.90m, 2);

        var act = () => produto.DebitarEstoque(5);

        var ex = act.Should().Throw<EstoqueInsuficienteException>().Which;
        ex.ProdutoId.Should().Be(7);
        ex.Solicitado.Should().Be(5);
        ex.Disponivel.Should().Be(2);
        produto.Estoque.Should().Be(2);
    }
}
