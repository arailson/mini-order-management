using System.Net;
using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Pedidos;

public class ListarPedidosTests : IntegrationTestBase
{
    public ListarPedidosTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetPedidos_SemFiltros_RetornaPaginaPadraoOrdenada()
    {
        var totalEsperado = await Fixture.ComConexaoAsync(
            c => DatabaseReset.ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Pedidos"));

        var response = await Client.GetAsync("/pedidos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await ReadJsonAsync<PagedDto>(response);
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.TotalItems.Should().Be(totalEsperado);
        page.TotalPages.Should().Be((int)Math.Ceiling(totalEsperado / 10.0));
        page.Items.Should().HaveCount(10);
        page.Items.Should().BeInDescendingOrder(i => i.DataPedido);
    }

    [Fact]
    public async Task GetPedidos_FiltroCliente_RetornaSomenteDoCliente()
    {
        var response = await Client.GetAsync("/pedidos?clienteId=9&pageSize=50");
        var page = await ReadJsonAsync<PagedDto>(response);

        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(i => i.ClienteId == 9 && i.ClienteNome == "Rafael Costa");
    }

    [Fact]
    public async Task GetPedidos_Periodo2026_RespeitaBordasInclusivas()
    {
        var response = await Client.GetAsync(
            "/pedidos?dataInicio=2026-01-01&dataFim=2026-12-31&pageSize=100");
        var page = await ReadJsonAsync<PagedDto>(response);

        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(i =>
            i.DataPedido >= new DateTime(2026, 1, 1) &&
            i.DataPedido < new DateTime(2027, 1, 1));
        page.Items.Should().Contain(i => i.DataPedido == new DateTime(2026, 1, 1, 0, 0, 0));
        page.Items.Should().Contain(i => i.DataPedido == new DateTime(2026, 12, 31, 23, 59, 59));
        page.Items.Should().NotContain(i => i.DataPedido.Year == 2025 || i.DataPedido.Year == 2027);
    }

    [Fact]
    public async Task GetPedidos_FiltrosCombinados_ClienteEPeriodo()
    {
        var response = await Client.GetAsync(
            "/pedidos?clienteId=1&dataInicio=2026-01-01&dataFim=2026-12-31&pageSize=100");
        var page = await ReadJsonAsync<PagedDto>(response);

        page!.Items.Should().OnlyContain(i =>
            i.ClienteId == 1 &&
            i.DataPedido.Year == 2026);
    }

    [Fact]
    public async Task GetPedidos_Pagina2_RetornaProximoBloco()
    {
        var p1 = await ReadJsonAsync<PagedDto>(await Client.GetAsync("/pedidos?page=1&pageSize=10"));
        var p2 = await ReadJsonAsync<PagedDto>(await Client.GetAsync("/pedidos?page=2&pageSize=10"));

        p2!.Page.Should().Be(2);
        p2.Items.Should().HaveCount(10);
        p2.Items.Select(i => i.Id).Should().NotIntersectWith(p1!.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetPedidos_PaginaAlemDoTotal_RetornaListaVazia()
    {
        var response = await Client.GetAsync("/pedidos?page=999&pageSize=10");
        var page = await ReadJsonAsync<PagedDto>(response);

        page!.Items.Should().BeEmpty();
        page.TotalItems.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPedidos_PageSizeMaiorQue100_Retorna400()
    {
        var response = await Client.GetAsync("/pedidos?pageSize=101");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPedidos_DatasInvertidas_Retorna400()
    {
        var response = await Client.GetAsync("/pedidos?dataInicio=2026-12-31&dataFim=2026-01-01");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("errors").TryGetProperty("dataInicio", out _).Should().BeTrue();
    }

    private sealed record PagedDto(
        List<ItemDto> Items,
        int Page,
        int PageSize,
        int TotalItems,
        int TotalPages);

    private sealed record ItemDto(
        int Id,
        int ClienteId,
        string ClienteNome,
        DateTime DataPedido,
        decimal ValorTotal);
}
