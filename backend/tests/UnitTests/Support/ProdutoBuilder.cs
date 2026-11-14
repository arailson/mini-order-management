using System.Reflection;
using Domain.Entities;

namespace UnitTests.Support;

/// <summary>Cria produtos de teste com Id atribuído via reflexão (setter privado).</summary>
internal static class ProdutoBuilder
{
    public static Produto Criar(int id, string nome, decimal preco, int estoque)
    {
        var produto = new Produto(nome, preco, estoque);
        typeof(Produto)
            .GetProperty(nameof(Produto.Id))!
            .SetValue(produto, id);
        return produto;
    }
}
