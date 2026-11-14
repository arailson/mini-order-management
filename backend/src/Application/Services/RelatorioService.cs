using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;

namespace Application.Services;

public interface IRelatorioService
{
    Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken);
}

public sealed class RelatorioService : IRelatorioService
{
    public const int QuantidadeTopClientes = 5;

    private readonly IPedidoRepository _pedidos;

    public RelatorioService(IPedidoRepository pedidos)
    {
        _pedidos = pedidos;
    }

    public async Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, string[]>();

        if (!dataInicio.HasValue)
        {
            erros["dataInicio"] = new[] { "A dataInicio é obrigatória." };
        }

        if (!dataFim.HasValue)
        {
            erros["dataFim"] = new[] { "A dataFim é obrigatória." };
        }

        if (dataInicio.HasValue && dataFim.HasValue && dataInicio.Value.Date > dataFim.Value.Date)
        {
            erros["dataInicio"] = new[] { "A dataInicio não pode ser posterior à dataFim." };
        }

        if (erros.Count > 0)
        {
            throw new ValidationException(erros);
        }

        // Intervalo [dataInicio, dataFim + 1 dia): inclui todo o último dia e permite uso de índice.
        return await _pedidos.ObterTopClientesAsync(
            dataInicio!.Value.Date,
            dataFim!.Value.Date.AddDays(1),
            QuantidadeTopClientes,
            cancellationToken);
    }
}
