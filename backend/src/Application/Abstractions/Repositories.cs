using Application.DTOs;
using Domain.Entities;

namespace Application.Abstractions;

public interface IClienteRepository
{
    Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClienteResponse>> ListarAsync(CancellationToken cancellationToken);
}

public interface IProdutoRepository
{
    /// <summary>
    /// Obtém os produtos informados (ordenados por Id) com bloqueio de atualização,
    /// para uso dentro de uma transação. Produtos inexistentes simplesmente não são retornados.
    /// </summary>
    Task<IReadOnlyList<Produto>> ObterParaAtualizacaoAsync(
        IReadOnlyCollection<int> produtoIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProdutoResponse>> ListarAsync(CancellationToken cancellationToken);
}

public interface IPedidoRepository
{
    void Adicionar(Pedido pedido);

    /// <param name="dataInicio">Limite inferior inclusivo (opcional).</param>
    /// <param name="dataFimExclusiva">Limite superior exclusivo (opcional).</param>
    Task<(IReadOnlyList<PedidoListItemResponse> Items, int TotalItems)> ListarAsync(
        int? clienteId,
        DateTime? dataInicio,
        DateTime? dataFimExclusiva,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <param name="dataInicio">Limite inferior inclusivo.</param>
    /// <param name="dataFimExclusiva">Limite superior exclusivo.</param>
    Task<IReadOnlyList<TopClienteResponse>> ObterTopClientesAsync(
        DateTime dataInicio,
        DateTime dataFimExclusiva,
        int quantidade,
        CancellationToken cancellationToken);
}
