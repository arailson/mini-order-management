namespace Domain.Entities;

public class Cliente
{
    private Cliente()
    {
        Nome = string.Empty;
        Email = string.Empty;
    }

    public Cliente(string nome, string email, DateTime dataCadastro)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new Exceptions.DomainException("O nome do cliente é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exceptions.DomainException("O e-mail do cliente é obrigatório.");
        }

        Nome = nome.Trim();
        Email = email.Trim();
        DataCadastro = dataCadastro;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; }

    public string Email { get; private set; }

    public DateTime DataCadastro { get; private set; }

    public ICollection<Pedido> Pedidos { get; private set; } = new List<Pedido>();
}
