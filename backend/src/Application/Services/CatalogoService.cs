using Application.Abstractions;
using Application.DTOs;

namespace Application.Services;

public interface ICatalogoService
{
    Task<IReadOnlyList<ClienteResponse>> ListarClientesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProdutoResponse>> ListarProdutosAsync(CancellationToken cancellationToken);
}

public sealed class CatalogoService : ICatalogoService
{
    private readonly IClienteRepository _clientes;
    private readonly IProdutoRepository _produtos;

    public CatalogoService(IClienteRepository clientes, IProdutoRepository produtos)
    {
        _clientes = clientes;
        _produtos = produtos;
    }

    public Task<IReadOnlyList<ClienteResponse>> ListarClientesAsync(CancellationToken cancellationToken) =>
        _clientes.ListarAsync(cancellationToken);

    public Task<IReadOnlyList<ProdutoResponse>> ListarProdutosAsync(CancellationToken cancellationToken) =>
        _produtos.ListarAsync(cancellationToken);
}
