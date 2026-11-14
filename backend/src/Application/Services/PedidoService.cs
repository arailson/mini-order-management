using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public interface IPedidoService
{
    Task<PedidoResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken);

    Task<PagedResult<PedidoListItemResponse>> ListarAsync(
        ConsultaPedidosFiltro filtro,
        CancellationToken cancellationToken);
}

public sealed class PedidoService : IPedidoService
{
    public const int PageSizePadrao = 10;
    public const int PageSizeMaximo = 100;

    private readonly IClienteRepository _clientes;
    private readonly IProdutoRepository _produtos;
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PedidoService(
        IClienteRepository clientes,
        IProdutoRepository produtos,
        IPedidoRepository pedidos,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _clientes = clientes;
        _produtos = produtos;
        _pedidos = pedidos;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PedidoResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken)
    {
        var itensConsolidados = ValidarEConsolidar(request);

        // Cliente, produtos, estoque, pedido, itens e baixa de estoque em uma única transação.
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (!await _clientes.ExisteAsync(request.ClienteId, ct))
            {
                throw new ClienteNaoEncontradoException(request.ClienteId);
            }

            var produtoIds = itensConsolidados.Select(i => i.ProdutoId).ToList();
            var produtos = (await _produtos.ObterParaAtualizacaoAsync(produtoIds, ct))
                .ToDictionary(p => p.Id);

            var pedido = new Pedido(request.ClienteId, _timeProvider.GetLocalNow().DateTime);

            foreach (var (produtoId, quantidade) in itensConsolidados)
            {
                if (!produtos.TryGetValue(produtoId, out var produto))
                {
                    throw new ProdutoNaoEncontradoException(produtoId);
                }

                // Captura o preço vigente, calcula o subtotal e debita o estoque (409 se insuficiente).
                pedido.AdicionarItem(produto, quantidade);
            }

            pedido.Validar();
            _pedidos.Adicionar(pedido);
            await _unitOfWork.SaveChangesAsync(ct);

            return ParaResponse(pedido);
        }, cancellationToken);
    }

    public async Task<PagedResult<PedidoListItemResponse>> ListarAsync(
        ConsultaPedidosFiltro filtro,
        CancellationToken cancellationToken)
    {
        var page = filtro.Page ?? 1;
        var pageSize = filtro.PageSize ?? PageSizePadrao;

        var erros = new Dictionary<string, string[]>();

        if (page < 1)
        {
            erros["page"] = new[] { "A página deve ser maior ou igual a 1." };
        }

        if (pageSize < 1 || pageSize > PageSizeMaximo)
        {
            erros["pageSize"] = new[] { $"O tamanho da página deve estar entre 1 e {PageSizeMaximo}." };
        }

        if (filtro.ClienteId is <= 0)
        {
            erros["clienteId"] = new[] { "O clienteId deve ser maior que zero." };
        }

        if (filtro.DataInicio.HasValue && filtro.DataFim.HasValue
            && filtro.DataInicio.Value.Date > filtro.DataFim.Value.Date)
        {
            erros["dataInicio"] = new[] { "A dataInicio não pode ser posterior à dataFim." };
        }

        if (erros.Count > 0)
        {
            throw new ValidationException(erros);
        }

        var dataInicio = filtro.DataInicio?.Date;
        var dataFimExclusiva = filtro.DataFim?.Date.AddDays(1);

        var (items, totalItems) = await _pedidos.ListarAsync(
            filtro.ClienteId,
            dataInicio,
            dataFimExclusiva,
            page,
            pageSize,
            cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<PedidoListItemResponse>(items, page, pageSize, totalItems, totalPages);
    }

    private static IReadOnlyList<(int ProdutoId, int Quantidade)> ValidarEConsolidar(CriarPedidoRequest? request)
    {
        if (request is null)
        {
            throw new ValidationException("O corpo da requisição é obrigatório.");
        }

        var erros = new Dictionary<string, string[]>();

        if (request.ClienteId <= 0)
        {
            erros["clienteId"] = new[] { "O clienteId é obrigatório e deve ser maior que zero." };
        }

        if (request.Itens is null || request.Itens.Count == 0)
        {
            erros["itens"] = new[] { "O pedido deve possuir pelo menos um item." };
        }
        else
        {
            for (var i = 0; i < request.Itens.Count; i++)
            {
                var item = request.Itens[i];

                if (item is null)
                {
                    erros[$"itens[{i}]"] = new[] { "Item inválido." };
                    continue;
                }

                if (item.ProdutoId <= 0)
                {
                    erros[$"itens[{i}].produtoId"] = new[] { "O produtoId é obrigatório e deve ser maior que zero." };
                }

                if (item.Quantidade <= 0)
                {
                    erros[$"itens[{i}].quantidade"] = new[] { "A quantidade deve ser maior que zero." };
                }
            }
        }

        if (erros.Count > 0)
        {
            throw new ValidationException(erros);
        }

        // Itens repetidos do mesmo produto são consolidados (soma das quantidades),
        // preservando a ordem da primeira ocorrência.
        return request.Itens!
            .GroupBy(i => i.ProdutoId)
            .Select(g => (ProdutoId: g.Key, Quantidade: g.Sum(i => i.Quantidade)))
            .ToList();
    }

    private static PedidoResponse ParaResponse(Pedido pedido) =>
        new(
            pedido.Id,
            pedido.ClienteId,
            pedido.DataPedido,
            pedido.ValorTotal,
            pedido.Itens
                .Select(i => new ItemPedidoResponse(i.ProdutoId, i.Quantidade, i.PrecoUnitario, i.Subtotal))
                .ToList());
}
