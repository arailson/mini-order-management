using Domain.Exceptions;

namespace Domain.Entities;

public class Produto
{
    private Produto()
    {
        Nome = string.Empty;
    }

    public Produto(string nome, decimal preco, int estoque)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("O nome do produto é obrigatório.");
        }

        if (preco <= 0)
        {
            throw new PrecoInvalidoException();
        }

        if (estoque < 0)
        {
            throw new EstoqueInvalidoException();
        }

        Nome = nome.Trim();
        Preco = preco;
        Estoque = estoque;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; }

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    /// <summary>Debita o estoque; lança exceção se a quantidade for inválida ou o saldo insuficiente.</summary>
    public void DebitarEstoque(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new QuantidadeInvalidaException();
        }

        if (Estoque < quantidade)
        {
            throw new EstoqueInsuficienteException(Id, Nome, quantidade, Estoque);
        }

        Estoque -= quantidade;
    }
}
