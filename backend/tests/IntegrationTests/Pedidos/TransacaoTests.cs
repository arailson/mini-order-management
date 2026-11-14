using Application.Abstractions;
using Domain.Entities;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Pedidos;

public class TransacaoTests : IntegrationTestBase
{
    public TransacaoTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ExecuteInTransaction_ExcecaoAposSaveChanges_NaoPersistePedido()
    {
        var pedidosAntes = await Fixture.ComConexaoAsync(
            c => DatabaseReset.ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Pedidos"));

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var pedidos = scope.ServiceProvider.GetRequiredService<IPedidoRepository>();

            var act = () => uow.ExecuteInTransactionAsync(async ct =>
            {
                pedidos.Adicionar(new Pedido(1, DateTime.UtcNow));
                await uow.SaveChangesAsync(ct);
                throw new InvalidOperationException("falha forçada");
#pragma warning disable CS0162
                return 0;
#pragma warning restore CS0162
            }, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        var pedidosDepois = await Fixture.ComConexaoAsync(
            c => DatabaseReset.ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Pedidos"));
        pedidosDepois.Should().Be(pedidosAntes);
    }
}
