using System.Net;
using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Relatorios;

public class TopClientesTests : IntegrationTestBase
{
    public TopClientesTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TopClientes_Periodo2026_RetornaRankingEsperado()
    {
        var response = await Client.GetAsync(
            "/relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ranking = await ReadJsonAsync<List<TopDto>>(response);

        ranking.Should().NotBeNull();
        var lista = ranking!;
        lista.Should().HaveCount(5);
        lista.Select(r => r.ClienteId).Should().Equal(1, 2, 3, 7, 4);

        var totaisSql = await Fixture.ComConexaoAsync(async connection =>
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT TOP 5 c.Id, CAST(ROUND(SUM(i.Quantidade * i.PrecoUnitario), 2) AS DECIMAL(18,2)) AS Total
                FROM dbo.Clientes c
                INNER JOIN dbo.Pedidos p ON p.ClienteId = c.Id
                INNER JOIN dbo.ItensPedido i ON i.PedidoId = p.Id
                WHERE p.DataPedido >= '2026-01-01' AND p.DataPedido < '2027-01-01'
                GROUP BY c.Id
                ORDER BY Total DESC, c.Id
                """;
            var list = new List<(int Id, decimal Total)>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add((reader.GetInt32(0), reader.GetDecimal(1)));
            }

            return list;
        });

        for (var i = 0; i < 5; i++)
        {
            lista[i].ClienteId.Should().Be(totaisSql[i].Id);
            lista[i].TotalComprado.Should().Be(totaisSql[i].Total);
            lista[i].TotalComprado.Should().Be(decimal.Round(lista[i].TotalComprado, 2));
        }
    }

    [Fact]
    public async Task TopClientes_PeriodoSemPedidos_RetornaListaVazia()
    {
        var response = await Client.GetAsync(
            "/relatorios/top-clientes?dataInicio=2030-01-01&dataFim=2030-12-31");
        var ranking = await ReadJsonAsync<List<TopDto>>(response);
        ranking.Should().BeEmpty();
    }

    [Fact]
    public async Task TopClientes_SemDatas_Retorna400()
    {
        var response = await Client.GetAsync("/relatorios/top-clientes");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TopClientes_IntervaloInvertido_Retorna400()
    {
        var response = await Client.GetAsync(
            "/relatorios/top-clientes?dataInicio=2026-12-31&dataFim=2026-01-01");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("errors").TryGetProperty("dataInicio", out _).Should().BeTrue();
    }

    private sealed record TopDto(int ClienteId, string Nome, decimal TotalComprado);
}
