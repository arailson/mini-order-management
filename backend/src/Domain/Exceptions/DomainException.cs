namespace Domain.Exceptions;

/// <summary>Violação de uma regra de negócio ou invariante do domínio (HTTP 400).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

public sealed class PrecoInvalidoException : DomainException
{
    public PrecoInvalidoException()
        : base("O preço do produto deve ser maior que zero.")
    {
    }
}

public sealed class EstoqueInvalidoException : DomainException
{
    public EstoqueInvalidoException()
        : base("O estoque não pode ser negativo.")
    {
    }
}

public sealed class QuantidadeInvalidaException : DomainException
{
    public QuantidadeInvalidaException()
        : base("A quantidade deve ser maior que zero.")
    {
    }
}

public sealed class PedidoSemItensException : DomainException
{
    public PedidoSemItensException()
        : base("O pedido deve possuir pelo menos um item.")
    {
    }
}
