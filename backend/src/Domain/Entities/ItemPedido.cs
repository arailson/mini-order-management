using Domain.Exceptions;

namespace Domain.Entities;

public class ItemPedido
{
    private ItemPedido()
    {
    }

    internal ItemPedido(Produto produto, int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new QuantidadeInvalidaException();
        }

        Produto = produto;
        ProdutoId = produto.Id;
        Quantidade = quantidade;
        // Preço vigente do produto no momento da criação do pedido.
        PrecoUnitario = produto.Preco;
    }

    public int Id { get; private set; }

    public int PedidoId { get; private set; }

    public int ProdutoId { get; private set; }

    public int Quantidade { get; private set; }

    public decimal PrecoUnitario { get; private set; }

    public decimal Subtotal => Quantidade * PrecoUnitario;

    public Pedido? Pedido { get; private set; }

    public Produto? Produto { get; private set; }
}
