using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using UnitTests.Support;

namespace UnitTests.Application;

public class PedidoServiceListarTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly PedidoService _sut;

    public PedidoServiceListarTests()
    {
        _sut = new PedidoService(
            Substitute.For<IClienteRepository>(),
            Substitute.For<IProdutoRepository>(),
            _pedidos,
            new FakeUnitOfWork(),
            new FakeTimeProvider());
    }

    [Fact]
    public async Task ListarAsync_FiltrosValidos_RetornaPaginacao()
    {
        var items = new List<PedidoListItemResponse>
        {
            new(1, 2, "Maria", new DateTime(2026, 5, 1), 100m),
        };
        _pedidos.ListarAsync(2, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1), 1, 10, Arg.Any<CancellationToken>())
            .Returns((items, 25));

        var resultado = await _sut.ListarAsync(
            new ConsultaPedidosFiltro(2, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), 1, 10),
            CancellationToken.None);

        resultado.Items.Should().HaveCount(1);
        resultado.Page.Should().Be(1);
        resultado.PageSize.Should().Be(10);
        resultado.TotalItems.Should().Be(25);
        resultado.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task ListarAsync_DataFimInclusiva_EnviaLimiteExclusivoAoRepositorio()
    {
        _pedidos.ListarAsync(
                null,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 2, 1),
                1,
                10,
                Arg.Any<CancellationToken>())
            .Returns((Array.Empty<PedidoListItemResponse>(), 0));

        await _sut.ListarAsync(
            new ConsultaPedidosFiltro(null, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), null, null),
            CancellationToken.None);

        await _pedidos.Received(1).ListarAsync(
            null,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 2, 1),
            1,
            PedidoService.PageSizePadrao,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListarAsync_PageInvalida_LancaValidationException(int page)
    {
        var act = () => _sut.ListarAsync(
            new ConsultaPedidosFiltro(null, null, null, page, 10),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ListarAsync_PageSizeInvalido_LancaValidationException(int pageSize)
    {
        var act = () => _sut.ListarAsync(
            new ConsultaPedidosFiltro(null, null, null, 1, pageSize),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("pageSize");
    }

    [Fact]
    public async Task ListarAsync_DataInicioPosteriorADataFim_LancaValidationException()
    {
        var act = () => _sut.ListarAsync(
            new ConsultaPedidosFiltro(null, new DateTime(2026, 12, 1), new DateTime(2026, 1, 1), 1, 10),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("dataInicio");
    }
}
