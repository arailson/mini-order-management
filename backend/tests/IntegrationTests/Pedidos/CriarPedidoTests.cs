using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;

namespace IntegrationTests.Pedidos;

public class CriarPedidoTests : IntegrationTestBase
{
    public CriarPedidoTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PostPedidos_DadosValidos_Retorna201EDebitaEstoque()
    {
        var estoqueAntes = await ObterEstoqueAsync(2);

        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = new[] { new { produtoId = 2, quantidade = 2 } },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var pedido = await ReadJsonAsync<PedidoDto>(response);
        pedido.Should().NotBeNull();
        pedido!.ClienteId.Should().Be(1);
        pedido.ValorTotal.Should().Be(179.80m);
        pedido.Itens.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ItemDto(2, 2, 89.90m, 179.80m));

        (await ObterEstoqueAsync(2)).Should().Be(estoqueAntes - 2);
        (await ContarPedidosAsync()).Should().Be(41);
    }

    [Fact]
    public async Task PostPedidos_ItensRepetidos_ConsolidaQuantidades()
    {
        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = new[]
            {
                new { produtoId = 2, quantidade = 1 },
                new { produtoId = 2, quantidade = 2 },
            },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var pedido = await ReadJsonAsync<PedidoDto>(response);
        pedido!.Itens.Should().ContainSingle().Which.Quantidade.Should().Be(3);
        pedido.ValorTotal.Should().Be(269.70m);
    }

    [Fact]
    public async Task PostPedidos_ClienteInexistente_Retorna404()
    {
        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 9999,
            itens = new[] { new { produtoId = 1, quantidade = 1 } },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadJsonAsync<ProblemDto>(response);
        problem!.Detail.Should().Contain("9999");
    }

    [Fact]
    public async Task PostPedidos_ProdutoInexistente_Retorna404()
    {
        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = new[] { new { produtoId = 9999, quantidade = 1 } },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostPedidos_EstoqueInsuficiente_Retorna409ComExtensoes()
    {
        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = new[] { new { produtoId = 10, quantidade = 999 } },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("produtoId").GetInt32().Should().Be(10);
        root.GetProperty("solicitado").GetInt32().Should().Be(999);
        root.GetProperty("disponivel").GetInt32().Should().Be(3);
        (await ContarPedidosAsync()).Should().Be(40);
    }

    [Fact]
    public async Task PostPedidos_SegundoItemSemEstoque_NaoAlteraEstoqueNemCriaPedido()
    {
        var estoqueNotebook = await ObterEstoqueAsync(1);
        var pedidosAntes = await ContarPedidosAsync();

        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = new[]
            {
                new { produtoId = 1, quantidade = 1 },
                new { produtoId = 10, quantidade = 999 },
            },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ObterEstoqueAsync(1)).Should().Be(estoqueNotebook);
        (await ContarPedidosAsync()).Should().Be(pedidosAntes);
    }

    [Fact]
    public async Task PostPedidos_SemItens_Retorna400()
    {
        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = Array.Empty<object>(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("errors").TryGetProperty("itens", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostPedidos_QuantidadeInvalida_Retorna400(int quantidade)
    {
        var response = await Client.PostAsync("/pedidos", JsonBody(new
        {
            clienteId = 1,
            itens = new[] { new { produtoId = 2, quantidade } },
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private Task<int> ObterEstoqueAsync(int produtoId) =>
        Fixture.ComConexaoAsync(c => DatabaseReset.ScalarIntAsync(c, $"SELECT Estoque FROM dbo.Produtos WHERE Id = {produtoId}"));

    private Task<int> ContarPedidosAsync() =>
        Fixture.ComConexaoAsync(c => DatabaseReset.ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Pedidos"));

    private sealed record PedidoDto(int Id, int ClienteId, DateTime DataPedido, decimal ValorTotal, List<ItemDto> Itens);
    private sealed record ItemDto(int ProdutoId, int Quantidade, decimal PrecoUnitario, decimal Subtotal);
    private sealed record ProblemDto(string? Title, int? Status, string? Detail);
}
