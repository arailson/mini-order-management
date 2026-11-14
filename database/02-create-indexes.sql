/* =============================================================================
   02-create-indexes.sql
   Mini Gestao de Pedidos - indices de apoio as consultas.

   Idempotente: cada indice so e criado se ainda nao existir.
   Pre-requisito: 01-create-tables.sql.
   ============================================================================= */

SET NOCOUNT ON;
GO

USE [MiniGestaoPedidos];
GO

/* Pedidos.ClienteId - filtro por cliente e join com Clientes */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Pedidos_ClienteId'
                 AND object_id = OBJECT_ID(N'dbo.Pedidos'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pedidos_ClienteId
        ON dbo.Pedidos (ClienteId);
END
GO

/* Pedidos.DataPedido - filtro por periodo e ordenacao por data */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_Pedidos_DataPedido'
                 AND object_id = OBJECT_ID(N'dbo.Pedidos'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pedidos_DataPedido
        ON dbo.Pedidos (DataPedido);
END
GO

/* ItensPedido.PedidoId - join com Pedidos */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_ItensPedido_PedidoId'
                 AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItensPedido_PedidoId
        ON dbo.ItensPedido (PedidoId);
END
GO

/* ItensPedido.ProdutoId - join com Produtos */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = N'IX_ItensPedido_ProdutoId'
                 AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItensPedido_ProdutoId
        ON dbo.ItensPedido (ProdutoId);
END
GO
