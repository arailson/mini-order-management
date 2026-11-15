/* =============================================================================
   perf-01-gerar-volume.sql
   Gera volume sintetico para comparar a query original e a otimizada.

   Alvo aproximado (alem do seed):
     - ~5.000 clientes adicionais
     - ~500.000 pedidos
     - ~1.500.000 itens (3 por pedido)

   Protecoes:
     1. Exige a propriedade estendida PerfExperiment = '1' (perf-00).
     2. Aborta se ja houver mais de 1.000 pedidos (evita reexecucao / banco
        de desenvolvimento com dados reais).

   Os Ids do seed (clientes 1-12, produtos 1-10, pedidos 1-40) permanecem.
   Datas espalhadas em 2025-2027, incluindo bordas do periodo 2026.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
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
        N'perf-01: abortado. Execute perf-00-marcar-experimento.sql primeiro (somente em banco descartavel).',
        16, 1);
    RETURN;
END;

DECLARE @pedidoCount INT = (SELECT COUNT(*) FROM dbo.Pedidos);
IF @pedidoCount > 1000
BEGIN
    RAISERROR(
        N'perf-01: abortado. Ja existem %d pedidos (> 1000). Parece volume ja gerado ou banco inadequado.',
        16, 1, @pedidoCount);
    RETURN;
END;

PRINT N'perf-01: iniciando geracao de volume. Pedidos atuais: ' + CAST(@pedidoCount AS NVARCHAR(20));

/* Tally 1..500000 (set-based, sem CROSS JOIN explosivo) */
IF OBJECT_ID(N'tempdb..#Nums') IS NOT NULL DROP TABLE #Nums;

;WITH
N0 AS (SELECT V FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS X(V)),
N1 AS (SELECT ones.V + 10 * tens.V + 100 * hundreds.V AS n
       FROM N0 ones CROSS JOIN N0 tens CROSS JOIN N0 hundreds), -- 0..999
N2 AS (SELECT a.n + 1000 * b.V AS n FROM N1 a CROSS JOIN N0 b), -- 0..9999
N3 AS (SELECT a.n + 10000 * b.V AS n FROM N2 a CROSS JOIN N0 b) -- 0..99999
SELECT TOP (500000) n + 1 AS n
INTO #Nums
FROM (
    SELECT a.n + 100000 * b.V AS n
    FROM N3 a
    CROSS JOIN (VALUES (0),(1),(2),(3),(4)) AS b(V) -- 0..499999
) x
ORDER BY n;

CREATE UNIQUE CLUSTERED INDEX IX_Nums ON #Nums (n);
PRINT N'perf-01: tally criado.';

DECLARE @seedPedidoMax INT = 40;
DECLARE @clienteMin INT;
DECLARE @clienteMax INT;
DECLARE @clienteSpan INT;
DECLARE @produtoMax INT = (SELECT MAX(Id) FROM dbo.Produtos);
DECLARE @batch INT;
DECLARE @batchSize INT = 50000;
DECLARE @maxN INT = 500000;
DECLARE @inserted INT;

BEGIN TRANSACTION;

/* ---------- Clientes adicionais (~5000) ---------- */
INSERT INTO dbo.Clientes (Nome, Email, DataCadastro)
SELECT
    CASE
        WHEN n % 50 = 1 THEN N'João Silva'
        WHEN n % 50 = 2 THEN N'Maria Souza'
        ELSE N'Cliente Perf ' + RIGHT(N'00000' + CAST(n AS NVARCHAR(10)), 5)
    END,
    N'perf.cliente.' + CAST(n AS NVARCHAR(20)) + N'@exemplo.com',
    DATEADD(DAY, -(n % 400), '2026-06-01T12:00:00')
FROM #Nums
WHERE n <= 5000;

PRINT N'perf-01: clientes inseridos: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT @clienteMin = MIN(Id), @clienteMax = MAX(Id) FROM dbo.Clientes;
SET @clienteSpan = @clienteMax - @clienteMin + 1;

/* ---------- Pedidos em lotes (~500000) ---------- */
SET @batch = 0;
WHILE @batch * @batchSize < @maxN
BEGIN
    INSERT INTO dbo.Pedidos (ClienteId, DataPedido, ValorTotal)
    SELECT
        @clienteMin + ((n - 1) % @clienteSpan),
        CASE
            WHEN n % 20 = 0  THEN CONVERT(DATETIME2, '2025-12-31T23:59:59')
            WHEN n % 20 = 1  THEN CONVERT(DATETIME2, '2026-01-01T00:00:00')
            WHEN n % 20 = 2  THEN CONVERT(DATETIME2, '2026-12-31T23:59:59')
            WHEN n % 20 = 3  THEN CONVERT(DATETIME2, '2027-01-01T00:00:00')
            WHEN n % 7  = 0  THEN DATEADD(DAY, (n % 365), CONVERT(DATETIME2, '2025-01-01T08:00:00'))
            WHEN n % 7  = 1  THEN DATEADD(DAY, (n % 365), CONVERT(DATETIME2, '2027-01-01T08:00:00'))
            ELSE DATEADD(MINUTE, (n % 525600), CONVERT(DATETIME2, '2026-01-01T00:00:00'))
        END,
        0
    FROM #Nums
    WHERE n > @batch * @batchSize
      AND n <= (@batch + 1) * @batchSize;

    SET @inserted = @@ROWCOUNT;
    SET @batch += 1;
    PRINT N'perf-01: lote de pedidos ' + CAST(@batch AS NVARCHAR(10))
        + N' (' + CAST(@inserted AS NVARCHAR(20)) + N' linhas).';
END;

/* ---------- Itens: 3 por pedido novo ---------- */
;WITH PedidosNovos AS (
    SELECT Id AS PedidoId
    FROM dbo.Pedidos
    WHERE Id > @seedPedidoMax
),
Slots AS (
    SELECT p.PedidoId, s.slot
    FROM PedidosNovos p
    CROSS JOIN (SELECT 1 AS slot UNION ALL SELECT 2 UNION ALL SELECT 3) s
)
INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT
    s.PedidoId,
    ((s.PedidoId + s.slot - 1) % @produtoMax) + 1,
    1 + ((s.PedidoId + s.slot) % 5),
    CAST(10.00 + ((s.PedidoId + s.slot * 7) % 990) AS DECIMAL(18,2))
FROM Slots s;

PRINT N'perf-01: itens inseridos: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

UPDATE p
SET ValorTotal = x.Total
FROM dbo.Pedidos p
INNER JOIN (
    SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total
    FROM dbo.ItensPedido
    WHERE PedidoId > @seedPedidoMax
    GROUP BY PedidoId
) x ON x.PedidoId = p.Id
WHERE p.Id > @seedPedidoMax;

PRINT N'perf-01: ValorTotal atualizado nos pedidos novos.';

COMMIT TRANSACTION;

UPDATE STATISTICS dbo.Clientes WITH FULLSCAN;
UPDATE STATISTICS dbo.Pedidos WITH FULLSCAN;
UPDATE STATISTICS dbo.ItensPedido WITH FULLSCAN;

PRINT N'perf-01: contagens finais:';
SELECT N'Clientes' AS Tabela, COUNT(*) AS Qtd FROM dbo.Clientes
UNION ALL SELECT N'Produtos', COUNT(*) FROM dbo.Produtos
UNION ALL SELECT N'Pedidos', COUNT(*) FROM dbo.Pedidos
UNION ALL SELECT N'ItensPedido', COUNT(*) FROM dbo.ItensPedido;

PRINT N'perf-01: concluido.';
GO
