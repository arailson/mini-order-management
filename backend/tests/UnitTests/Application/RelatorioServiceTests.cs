using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Services;
using FluentAssertions;
using NSubstitute;

namespace UnitTests.Application;

public class RelatorioServiceTests
{
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly RelatorioService _sut;

    public RelatorioServiceTests()
    {
        _sut = new RelatorioService(_pedidos);
    }

    [Fact]
    public async Task TopClientesAsync_PeriodoValido_RepassaIntervaloExclusivoEQuantidade()
    {
        var ranking = new List<TopClienteResponse>
        {
            new(1, "João", 1000m),
        };
        _pedidos.ObterTopClientesAsync(
                new DateTime(2026, 1, 1),
                new DateTime(2027, 1, 1),
                RelatorioService.QuantidadeTopClientes,
                Arg.Any<CancellationToken>())
            .Returns(ranking);

        var resultado = await _sut.TopClientesAsync(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            CancellationToken.None);

        resultado.Should().BeEquivalentTo(ranking);
        await _pedidos.Received(1).ObterTopClientesAsync(
            new DateTime(2026, 1, 1),
            new DateTime(2027, 1, 1),
            5,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TopClientesAsync_SemDataInicio_LancaValidationException()
    {
        var act = () => _sut.TopClientesAsync(null, new DateTime(2026, 12, 31), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("dataInicio");
    }

    [Fact]
    public async Task TopClientesAsync_SemDataFim_LancaValidationException()
    {
        var act = () => _sut.TopClientesAsync(new DateTime(2026, 1, 1), null, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("dataFim");
    }

    [Fact]
    public async Task TopClientesAsync_IntervaloInvertido_LancaValidationException()
    {
        var act = () => _sut.TopClientesAsync(
            new DateTime(2026, 12, 31),
            new DateTime(2026, 1, 1),
            CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("dataInicio");
    }
}
