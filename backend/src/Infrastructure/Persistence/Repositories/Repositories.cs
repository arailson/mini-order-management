using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _db;

    public ClienteRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken) =>
        _db.Clientes.AsNoTracking().AnyAsync(c => c.Id == clienteId, cancellationToken);

    public async Task<IReadOnlyList<ClienteResponse>> ListarAsync(CancellationToken cancellationToken) =>
        await _db.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Id)
            .Select(c => new ClienteResponse(c.Id, c.Nome, c.Email, c.DataCadastro))
            .ToListAsync(cancellationToken);
}

internal sealed class ProdutoRepository : IProdutoRepository
{
    private readonly AppDbContext _db;

    public ProdutoRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Produto>> ObterParaAtualizacaoAsync(
        IReadOnlyCollection<int> produtoIds,
        CancellationToken cancellationToken)
    {
        // Ordenar por Id evita deadlocks entre pedidos concorrentes que disputam os mesmos produtos.
        var ids = produtoIds.Distinct().OrderBy(id => id).ToList();

        if (ids.Count == 0)
        {
            return Array.Empty<Produto>();
        }

        // UPDLOCK/ROWLOCK mantêm a linha bloqueada até o fim da transação explícita,
        // impedindo que dois pedidos debitem o mesmo saldo simultaneamente.
        var placeholders = string.Join(", ", ids.Select((_, index) => $"{{{index}}}"));
        var sql = $"SELECT Id, Nome, Preco, Estoque FROM dbo.Produtos WITH (UPDLOCK, ROWLOCK) WHERE Id IN ({placeholders})";

        return await _db.Produtos
            .FromSqlRaw(sql, ids.Cast<object>().ToArray())
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProdutoResponse>> ListarAsync(CancellationToken cancellationToken) =>
        await _db.Produtos
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .ThenBy(p => p.Id)
            .Select(p => new ProdutoResponse(p.Id, p.Nome, p.Preco, p.Estoque))
            .ToListAsync(cancellationToken);
}

internal sealed class PedidoRepository : IPedidoRepository
{
    private readonly AppDbContext _db;

    public PedidoRepository(AppDbContext db)
    {
        _db = db;
    }

    public void Adicionar(Pedido pedido) => _db.Pedidos.Add(pedido);

    public async Task<(IReadOnlyList<PedidoListItemResponse> Items, int TotalItems)> ListarAsync(
        int? clienteId,
        DateTime? dataInicio,
        DateTime? dataFimExclusiva,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Pedidos.AsNoTracking().AsQueryable();

        if (clienteId.HasValue)
        {
            query = query.Where(p => p.ClienteId == clienteId.Value);
        }

        // Comparações diretas na coluna (sem funções) para permitir uso do índice em DataPedido.
        if (dataInicio.HasValue)
        {
            query = query.Where(p => p.DataPedido >= dataInicio.Value);
        }

        if (dataFimExclusiva.HasValue)
        {
            query = query.Where(p => p.DataPedido < dataFimExclusiva.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.DataPedido)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PedidoListItemResponse(
                p.Id,
                p.ClienteId,
                p.Cliente!.Nome,
                p.DataPedido,
                p.ValorTotal))
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<TopClienteResponse>> ObterTopClientesAsync(
        DateTime dataInicio,
        DateTime dataFimExclusiva,
        int quantidade,
        CancellationToken cancellationToken)
    {
        var ranking = await _db.ItensPedido
            .AsNoTracking()
            .Where(i => i.Pedido!.DataPedido >= dataInicio && i.Pedido.DataPedido < dataFimExclusiva)
            .GroupBy(i => new { i.Pedido!.ClienteId, i.Pedido.Cliente!.Nome })
            .Select(g => new
            {
                g.Key.ClienteId,
                g.Key.Nome,
                Total = g.Sum(i => i.Quantidade * i.PrecoUnitario),
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.ClienteId)
            .Take(quantidade)
            .ToListAsync(cancellationToken);

        return ranking
            .Select(x => new TopClienteResponse(x.ClienteId, x.Nome, Math.Round(x.Total, 2)))
            .ToList();
    }
}
