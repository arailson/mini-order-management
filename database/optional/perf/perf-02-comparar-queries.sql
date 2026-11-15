/* =============================================================================
   perf-02-comparar-queries.sql
   Compara a query original (enunciado), uma variante intermediaria e a
   otimizada (spec secao 7), com STATISTICS IO/TIME e planos reais.

   Requer volume gerado (perf-01) para diferencas relevantes de I/O.
   Em banco so com seed, ainda e util para validar corretude do resultado.
   ============================================================================= */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE [MiniGestaoPedidos];
GO

PRINT N'========== Contagens ==========';
SELECT N'Clientes' AS Tabela, COUNT(*) AS Qtd FROM dbo.Clientes
UNION ALL SELECT N'Produtos', COUNT(*) FROM dbo.Produtos
UNION ALL SELECT N'Pedidos', COUNT(*) FROM dbo.Pedidos
UNION ALL SELECT N'ItensPedido', COUNT(*) FROM dbo.ItensPedido;

PRINT N'========== Indices em Pedidos / ItensPedido / Clientes ==========';
SELECT
    OBJECT_NAME(i.object_id) AS Tabela,
    i.name AS Indice,
    i.type_desc,
    COL_NAME(ic.object_id, ic.column_id) AS Coluna,
    ic.is_included_column AS Incluida,
    ic.key_ordinal AS Ordem
FROM sys.indexes i
INNER JOIN sys.index_columns ic
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
WHERE i.object_id IN (OBJECT_ID(N'dbo.Pedidos'), OBJECT_ID(N'dbo.ItensPedido'), OBJECT_ID(N'dbo.Clientes'))
  AND i.name IS NOT NULL
ORDER BY Tabela, Indice, Incluida, Ordem;

PRINT N'========== Corretude: original (amostra TOP 10 pela ordem ASC) ==========';
SELECT TOP 10 c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total;

PRINT N'========== Corretude: otimizada (TOP 5) ==========';
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
    Total DESC;

/* ---------- Medicoes com cache frio ---------- */
DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;
DBCC FREEPROCCACHE WITH NO_INFOMSGS;

SET STATISTICS IO ON;
SET STATISTICS TIME ON;

/* MAXDOP 1 para comparacao reproduzivel entre variantes. */
PRINT N'========== MEDICAO 1: ORIGINAL (enunciado) ==========';
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

PRINT N'========== MEDICAO 2: INTERMEDIARIA (range no lugar de YEAR) ==========';
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

PRINT N'========== MEDICAO 3: OTIMIZADA (spec) ==========';
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

PRINT N'========== Planos reais (XML) ==========';
SET STATISTICS XML ON;

PRINT N'--- Plano real: ORIGINAL ---';
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
OPTION (MAXDOP 1);

PRINT N'--- Plano real: OTIMIZADA ---';
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

SET STATISTICS XML OFF;

PRINT N'perf-02: concluido.';
GO
