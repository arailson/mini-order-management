using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using UnitTests.Support;

namespace UnitTests.Application;

public class PedidoServiceCriarTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.FromHours(-3)));
    private readonly PedidoService _sut;

    public PedidoServiceCriarTests()
    {
        _sut = new PedidoService(_clientes, _produtos, _pedidos, _uow, _clock);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_RetornaPedidoComPrecoVigente()
    {
        var mouse = ProdutoBuilder.Criar(2, "Mouse", 80m, 20);
        ConfigurarClienteExiste(1);
        ConfigurarProdutos(mouse);

        var response = await _sut.CriarAsync(
            new CriarPedidoRequest(1, [new ItemPedidoRequest(2, 3)]),
            CancellationToken.None);

        response.ClienteId.Should().Be(1);
        response.DataPedido.Should().Be(new DateTime(2026, 3, 15, 10, 30, 0));
        response.ValorTotal.Should().Be(240m);
        response.Itens.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ItemPedidoResponse(2, 3, 80m, 240m));
        _pedidos.Received(1).Adicionar(Arg.Any<Pedido>());
        _uow.SaveChangesCalls.Should().Be(1);
        _uow.CommitCount.Should().Be(1);
        mouse.Estoque.Should().Be(17);
    }

    [Fact]
    public async Task CriarAsync_ClienteInexistente_LancaClienteNaoEncontradoException()
    {
        _clientes.ExisteAsync(999, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(999, [new ItemPedidoRequest(1, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ClienteNaoEncontradoException>();
        _pedidos.DidNotReceive().Adicionar(Arg.Any<Pedido>());
        _uow.SaveChangesCalls.Should().Be(0);
        _uow.RollbackCount.Should().Be(1);
    }

    [Fact]
    public async Task CriarAsync_ProdutoInexistente_LancaProdutoNaoEncontradoException()
    {
        ConfigurarClienteExiste(1);
        _produtos.ObterParaAtualizacaoAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Produto>());

        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(1, [new ItemPedidoRequest(55, 1)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ProdutoNaoEncontradoException>()
            .Where(e => e.ProdutoId == 55);
        _pedidos.DidNotReceive().Adicionar(Arg.Any<Pedido>());
        _uow.SaveChangesCalls.Should().Be(0);
        _uow.RollbackCount.Should().Be(1);
    }

    [Fact]
    public async Task CriarAsync_EstoqueInsuficiente_LancaEstoqueInsuficienteException()
    {
        var cabo = ProdutoBuilder.Criar(10, "Cabo HDMI", 29.90m, 2);
        ConfigurarClienteExiste(1);
        ConfigurarProdutos(cabo);

        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(1, [new ItemPedidoRequest(10, 5)]),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<EstoqueInsuficienteException>()).Which;
        ex.ProdutoId.Should().Be(10);
        ex.Solicitado.Should().Be(5);
        ex.Disponivel.Should().Be(2);
        _pedidos.DidNotReceive().Adicionar(Arg.Any<Pedido>());
        _uow.SaveChangesCalls.Should().Be(0);
        _uow.RollbackCount.Should().Be(1);
        cabo.Estoque.Should().Be(2);
    }

    [Fact]
    public async Task CriarAsync_SemItens_LancaValidationException()
    {
        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(1, []),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("itens");
        await _clientes.DidNotReceive().ExisteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _uow.CommitCount.Should().Be(0);
        _uow.RollbackCount.Should().Be(0);
    }

    [Fact]
    public async Task CriarAsync_ItensNulos_LancaValidationException()
    {
        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(1, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("itens"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CriarAsync_QuantidadeInvalida_LancaValidationException(int quantidade)
    {
        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(1, [new ItemPedidoRequest(2, quantidade)]),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("itens[0].quantidade");
        await _clientes.DidNotReceive().ExisteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CriarAsync_ClienteIdInvalido_LancaValidationException(int clienteId)
    {
        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(clienteId, [new ItemPedidoRequest(1, 1)]),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("clienteId");
    }

    [Fact]
    public async Task CriarAsync_VariosItens_CalculaValorTotalCorreto()
    {
        var notebook = ProdutoBuilder.Criar(1, "Notebook", 2500m, 5);
        var mouse = ProdutoBuilder.Criar(2, "Mouse", 49.90m, 20);
        ConfigurarClienteExiste(3);
        ConfigurarProdutos(notebook, mouse);

        var response = await _sut.CriarAsync(
            new CriarPedidoRequest(3, [
                new ItemPedidoRequest(1, 1),
                new ItemPedidoRequest(2, 2),
            ]),
            CancellationToken.None);

        response.ValorTotal.Should().Be(2599.80m);
        response.Itens.Should().HaveCount(2);
        notebook.Estoque.Should().Be(4);
        mouse.Estoque.Should().Be(18);
    }

    [Fact]
    public async Task CriarAsync_ItensRepetidos_ConsolidaQuantidades()
    {
        var mouse = ProdutoBuilder.Criar(2, "Mouse", 100m, 50);
        ConfigurarClienteExiste(1);
        ConfigurarProdutos(mouse);

        var response = await _sut.CriarAsync(
            new CriarPedidoRequest(1, [
                new ItemPedidoRequest(2, 2),
                new ItemPedidoRequest(2, 3),
            ]),
            CancellationToken.None);

        response.Itens.Should().ContainSingle()
            .Which.Quantidade.Should().Be(5);
        response.ValorTotal.Should().Be(500m);
        mouse.Estoque.Should().Be(45);
    }

    [Fact]
    public async Task CriarAsync_SegundoItemSemEstoque_NaoPersisteNemCommit()
    {
        var ok = ProdutoBuilder.Criar(1, "Notebook", 2000m, 10);
        var baixo = ProdutoBuilder.Criar(10, "Cabo", 30m, 1);
        ConfigurarClienteExiste(1);
        ConfigurarProdutos(ok, baixo);

        var act = () => _sut.CriarAsync(
            new CriarPedidoRequest(1, [
                new ItemPedidoRequest(1, 1),
                new ItemPedidoRequest(10, 5),
            ]),
            CancellationToken.None);

        await act.Should().ThrowAsync<EstoqueInsuficienteException>();
        _pedidos.DidNotReceive().Adicionar(Arg.Any<Pedido>());
        _uow.SaveChangesCalls.Should().Be(0);
        _uow.RollbackCount.Should().Be(1);
        _uow.CommitCount.Should().Be(0);
    }

    private void ConfigurarClienteExiste(int clienteId) =>
        _clientes.ExisteAsync(clienteId, Arg.Any<CancellationToken>()).Returns(true);

    private void ConfigurarProdutos(params Produto[] produtos) =>
        _produtos.ObterParaAtualizacaoAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(produtos);
}
