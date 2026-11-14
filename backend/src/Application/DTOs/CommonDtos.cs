namespace Application.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record TopClienteResponse(int ClienteId, string Nome, decimal TotalComprado);

public sealed record ClienteResponse(int Id, string Nome, string Email, DateTime DataCadastro);

public sealed record ProdutoResponse(int Id, string Nome, decimal Preco, int Estoque);
