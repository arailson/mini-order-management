/* =============================================================================
   perf-02b-medicao-io.sql
   Medicao isolada de STATISTICS IO/TIME (MAXDOP 1, cache frio).
   Result sets sao escritos; o pos-processamento extrai apenas as mensagens.
   ============================================================================= */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE [MiniGestaoPedidos];
GO

DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT N'###MEDICAO###ORIGINAL###';
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
OPTION (MAXDOP 1);

DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;

PRINT N'###MEDICAO###INTERMEDIARIA###';
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE p.DataPedido >= '20260101'
  AND p.DataPedido < '20270101'
GROUP BY c.Nome
ORDER BY Total
OPTION (MAXDOP 1);

DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;

PRINT N'###MEDICAO###OTIMIZADA###';
SELECT TOP 5
    c.Id AS ClienteId,
    c.Nome,
    SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
INNER JOIN Pedidos p ON p.ClienteId = c.Id
INNER JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE p.DataPedido >= '20260101'
  AND p.DataPedido < '20270101'
GROUP BY c.Id, c.Nome
ORDER BY Total DESC
OPTION (MAXDOP 1);

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
PRINT N'###MEDICAO###FIM###';
GO
