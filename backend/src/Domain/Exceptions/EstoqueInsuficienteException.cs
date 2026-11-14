namespace Domain.Exceptions;

/// <summary>Estoque disponível não atende à quantidade solicitada (HTTP 409).</summary>
public sealed class EstoqueInsuficienteException : Exception
{
    public EstoqueInsuficienteException(int produtoId, string produtoNome, int solicitado, int disponivel)
        : base($"Estoque insuficiente para o produto {produtoId} ({produtoNome}): solicitado {solicitado}, disponível {disponivel}.")
    {
        ProdutoId = produtoId;
        Solicitado = solicitado;
        Disponivel = disponivel;
    }

    public int ProdutoId { get; }

    public int Solicitado { get; }

    public int Disponivel { get; }
}
