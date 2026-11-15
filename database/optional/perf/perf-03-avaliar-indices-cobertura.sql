/* =============================================================================
   perf-03-avaliar-indices-cobertura.sql
   Avalia indices de cobertura opcionais (NAO altera 02-create-indexes.sql).

   Cria temporariamente:
     - IX_Pedidos_DataPedido_InclCliente (DataPedido INCLUDE ClienteId)
     - IX_ItensPedido_PedidoId_InclQtdPreco (PedidoId INCLUDE Quantidade, PrecoUnitario)

   Remede a query otimizada e remove os indices ao final.
   ============================================================================= */

SET NOCOUNT ON;
GO

USE [MiniGestaoPedidos];
GO

DECLARE @perfFlag SQL_VARIANT =
(
    SELECT value
    FROM sys.extended_properties
    WHERE class = 0
      AND name = N'PerfExperiment'
);

IF @perfFlag IS NULL OR CONVERT(NVARCHAR(10), @perfFlag) <> N'1'
BEGIN
    RAISERROR(
        N'perf-03: abortado. Somente em banco marcado com PerfExperiment=1.',
        16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Pedidos_DataPedido_InclCliente'
      AND object_id = OBJECT_ID(N'dbo.Pedidos')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pedidos_DataPedido_InclCliente
        ON dbo.Pedidos (DataPedido)
        INCLUDE (ClienteId);
    PRINT N'perf-03: criado IX_Pedidos_DataPedido_InclCliente.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ItensPedido_PedidoId_InclQtdPreco'
      AND object_id = OBJECT_ID(N'dbo.ItensPedido')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItensPedido_PedidoId_InclQtdPreco
        ON dbo.ItensPedido (PedidoId)
        INCLUDE (Quantidade, PrecoUnitario);
    PRINT N'perf-03: criado IX_ItensPedido_PedidoId_InclQtdPreco.';
END;

UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN;
UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;

DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;

SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT N'========== MEDICAO: OTIMIZADA com indices de cobertura (MAXDOP 1) ==========';
SELECT TOP 5
    c.Id AS ClienteId,
    c.Nome,
    SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
INNER JOIN Pedidos p
    ON p.ClienteId = c.Id
INNER JOIN ItensPedido i
    ON i.PedidoId = p.Id
WHERE
    p.DataPedido >= '20260101'
    AND p.DataPedido < '20270101'
GROUP BY
    c.Id,
    c.Nome
ORDER BY
    Total DESC
OPTION (MAXDOP 1);

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

/* Remove indices experimentais (banco descartavel) */
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Pedidos_DataPedido_InclCliente'
      AND object_id = OBJECT_ID(N'dbo.Pedidos')
)
    DROP INDEX IX_Pedidos_DataPedido_InclCliente ON dbo.Pedidos;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ItensPedido_PedidoId_InclQtdPreco'
      AND object_id = OBJECT_ID(N'dbo.ItensPedido')
)
    DROP INDEX IX_ItensPedido_PedidoId_InclQtdPreco ON dbo.ItensPedido;

PRINT N'perf-03: indices de cobertura removidos. Concluido.';
GO
