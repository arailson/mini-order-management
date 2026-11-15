/* =============================================================================
   perf-04-extrair-planos.sql
   Planos estimados (SHOWPLAN_TEXT) e resumo de operadores dos planos reais.
   ============================================================================= */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE [MiniGestaoPedidos];
GO

PRINT N'========== Plano ESTIMADO: ORIGINAL ==========';
GO
SET SHOWPLAN_TEXT ON;
GO
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
OPTION (MAXDOP 1);
GO
SET SHOWPLAN_TEXT OFF;
GO

PRINT N'========== Plano ESTIMADO: OTIMIZADA ==========';
GO
SET SHOWPLAN_TEXT ON;
GO
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
GO
SET SHOWPLAN_TEXT OFF;
GO

DBCC FREEPROCCACHE WITH NO_INFOMSGS;
GO

/* Execucao para popular cache de planos reais */
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
OPTION (MAXDOP 1);
GO

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
GO

PRINT N'========== Resumo planos reais (DMV) ==========';
;WITH plans AS (
    SELECT TOP 20
        qs.creation_time,
        qs.execution_count,
        qs.total_logical_reads,
        qs.total_worker_time,
        qs.total_elapsed_time,
        st.text AS query_text,
        qp.query_plan
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
    CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
    WHERE st.text LIKE N'%SUM(i.Quantidade * i.PrecoUnitario)%'
      AND st.text NOT LIKE N'%dm_exec_query_stats%'
    ORDER BY qs.creation_time DESC
)
SELECT
    CASE
        WHEN query_text LIKE N'%YEAR(p.DataPedido)%' THEN N'ORIGINAL'
        WHEN query_text LIKE N'%SELECT TOP%5%' OR query_text LIKE N'%TOP 5%' THEN N'OTIMIZADA'
        ELSE N'OUTRA'
    END AS Variante,
    execution_count,
    total_logical_reads,
    total_worker_time AS worker_time_us,
    total_elapsed_time AS elapsed_time_us
FROM plans
ORDER BY creation_time DESC;
GO

PRINT N'========== Operadores dos planos reais ==========';
;WITH xmlplans AS (
    SELECT TOP 10
        qs.creation_time,
        st.text AS query_text,
        CAST(qp.query_plan AS XML) AS plan_xml
    FROM sys.dm_exec_query_stats qs
    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
    CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
    WHERE st.text LIKE N'%SUM(i.Quantidade * i.PrecoUnitario)%'
      AND st.text NOT LIKE N'%dm_exec_query_stats%'
    ORDER BY qs.creation_time DESC
)
SELECT
    CASE
        WHEN query_text LIKE N'%YEAR(p.DataPedido)%' THEN N'ORIGINAL'
        WHEN query_text LIKE N'%TOP 5%' THEN N'OTIMIZADA'
        ELSE N'OUTRA'
    END AS Variante,
    rel.value(N'@NodeId', N'INT') AS NodeId,
    rel.value(N'@PhysicalOp', N'NVARCHAR(60)') AS PhysicalOp,
    rel.value(N'@LogicalOp', N'NVARCHAR(60)') AS LogicalOp,
    CAST(rel.value(N'@EstimateRows', N'FLOAT') AS DECIMAL(18,2)) AS EstimateRows,
    CAST(rel.value(N'@EstimatedTotalSubtreeCost', N'FLOAT') AS DECIMAL(18,6)) AS SubtreeCost
FROM xmlplans
CROSS APPLY plan_xml.nodes(N'//RelOp') AS q(rel)
ORDER BY Variante, NodeId;
GO
