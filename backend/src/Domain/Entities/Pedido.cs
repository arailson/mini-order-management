using Domain.Exceptions;

namespace Domain.Entities;

public class Pedido
{
    private readonly List<ItemPedido> _itens = new();

    private Pedido()
    {
    }

    public Pedido(int clienteId, DateTime dataPedido)
    {
        ClienteId = clienteId;
        DataPedido = dataPedido;
    }

    public int Id { get; private set; }

    public int ClienteId { get; private set; }

    public DateTime DataPedido { get; private set; }

    public decimal ValorTotal { get; private set; }

    public Cliente? Cliente { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens;

    /// <summary>
    /// Adiciona um item capturando o preço vigente do produto, debita o estoque
    /// e recalcula o valor total do pedido.
    /// </summary>
    public ItemPedido AdicionarItem(Produto produto, int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new QuantidadeInvalidaException();
        }

        produto.DebitarEstoque(quantidade);

        var item = new ItemPedido(produto, quantidade);
        _itens.Add(item);
        RecalcularTotal();
        return item;
    }

    /// <summary>Garante as invariantes do pedido antes da persistência.</summary>
    public void Validar()
    {
        if (_itens.Count == 0)
        {
            throw new PedidoSemItensException();
        }
    }

    private void RecalcularTotal()
    {
        ValorTotal = _itens.Sum(i => i.Subtotal);
    }
}
