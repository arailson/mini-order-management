namespace Application.DTOs;

public sealed record ItemPedidoRequest(int ProdutoId, int Quantidade);

public sealed record CriarPedidoRequest(int ClienteId, IReadOnlyList<ItemPedidoRequest>? Itens);

public sealed record ItemPedidoResponse(
    int ProdutoId,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public sealed record PedidoResponse(
    int Id,
    int ClienteId,
    DateTime DataPedido,
    decimal ValorTotal,
    IReadOnlyList<ItemPedidoResponse> Itens);

public sealed record PedidoListItemResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal);

public sealed record ConsultaPedidosFiltro(
    int? ClienteId,
    DateTime? DataInicio,
    DateTime? DataFim,
    int? Page,
    int? PageSize);
