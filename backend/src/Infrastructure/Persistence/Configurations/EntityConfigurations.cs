using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

// Mapeamentos alinhados a database/01-create-tables.sql e database/02-create-indexes.sql.

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes", "dbo");

        builder.HasKey(c => c.Id).HasName("PK_Clientes");
        builder.Property(c => c.Id).UseIdentityColumn(1, 1);

        builder.Property(c => c.Nome)
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.DataCadastro)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSDATETIME()")
            .IsRequired();
    }
}

internal sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos", "dbo", t =>
        {
            t.HasCheckConstraint("CK_Produtos_Preco", "[Preco] > 0");
            t.HasCheckConstraint("CK_Produtos_Estoque", "[Estoque] >= 0");
        });

        builder.HasKey(p => p.Id).HasName("PK_Produtos");
        builder.Property(p => p.Id).UseIdentityColumn(1, 1);

        builder.Property(p => p.Nome)
            .HasColumnType("nvarchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Preco)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Estoque)
            .HasColumnType("int")
            .IsRequired();
    }
}

internal sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos", "dbo");

        builder.HasKey(p => p.Id).HasName("PK_Pedidos");
        builder.Property(p => p.Id).UseIdentityColumn(1, 1);

        builder.Property(p => p.ClienteId).IsRequired();

        builder.Property(p => p.DataPedido)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSDATETIME()")
            .IsRequired();

        builder.Property(p => p.ValorTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(p => p.Cliente)
            .WithMany(c => c.Pedidos)
            .HasForeignKey(p => p.ClienteId)
            .HasConstraintName("FK_Pedidos_Clientes")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(p => p.ClienteId).HasDatabaseName("IX_Pedidos_ClienteId");
        builder.HasIndex(p => p.DataPedido).HasDatabaseName("IX_Pedidos_DataPedido");

        // Itens expostos como somente leitura; o EF usa o campo privado _itens.
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItensPedido", "dbo", t =>
        {
            t.HasCheckConstraint("CK_ItensPedido_Quantidade", "[Quantidade] > 0");
        });

        builder.HasKey(i => i.Id).HasName("PK_ItensPedido");
        builder.Property(i => i.Id).UseIdentityColumn(1, 1);

        builder.Property(i => i.PedidoId).IsRequired();
        builder.Property(i => i.ProdutoId).IsRequired();

        builder.Property(i => i.Quantidade)
            .HasColumnType("int")
            .IsRequired();

        builder.Property(i => i.PrecoUnitario)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Propriedade calculada, não persistida.
        builder.Ignore(i => i.Subtotal);

        builder.HasOne(i => i.Pedido)
            .WithMany(p => p.Itens)
            .HasForeignKey(i => i.PedidoId)
            .HasConstraintName("FK_ItensPedido_Pedidos")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .HasConstraintName("FK_ItensPedido_Produtos")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(i => i.PedidoId).HasDatabaseName("IX_ItensPedido_PedidoId");
        builder.HasIndex(i => i.ProdutoId).HasDatabaseName("IX_ItensPedido_ProdutoId");
    }
}
