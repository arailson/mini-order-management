using FluentAssertions;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Smoke;

public class CatalogoSmokeTests : IntegrationTestBase
{
    public CatalogoSmokeTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetClientes_SeedAplicado_Retorna12Clientes()
    {
        var response = await Client.GetAsync("/clientes");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var clientes = await ReadJsonAsync<List<ClienteDto>>(response);
        clientes.Should().NotBeNull();
        clientes!.Should().HaveCount(12);
    }

    private sealed record ClienteDto(int Id, string Nome, string Email, DateTime DataCadastro);
}
