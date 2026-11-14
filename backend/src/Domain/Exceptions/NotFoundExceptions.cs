namespace Domain.Exceptions;

/// <summary>Recurso inexistente (HTTP 404).</summary>
public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message) : base(message)
    {
    }
}

public sealed class ClienteNaoEncontradoException : NotFoundException
{
    public ClienteNaoEncontradoException(int clienteId)
        : base($"Cliente {clienteId} não encontrado.")
    {
        ClienteId = clienteId;
    }

    public int ClienteId { get; }
}

public sealed class ProdutoNaoEncontradoException : NotFoundException
{
    public ProdutoNaoEncontradoException(int produtoId)
        : base($"Produto {produtoId} não encontrado.")
    {
        ProdutoId = produtoId;
    }

    public int ProdutoId { get; }
}
