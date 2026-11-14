/* =============================================================================
   03-seed-data.sql
   Mini Gestao de Pedidos - massa de dados de exemplo.

   Protegido para banco novo: so insere se a tabela Clientes estiver vazia.
   Pre-requisitos: 01-create-tables.sql e 02-create-indexes.sql.

   Cenarios cobertos:
     - Clientes com nomes iguais e e-mails diferentes
         (Joao Silva: Ids 1 e 5 | Maria Souza: Ids 2 e 6).
     - Clientes sem pedidos (Ids 10, 11 e 12).
     - Mais de 25 pedidos em 2026 (paginacao com pageSize = 10).
     - Pedidos fora de 2026 (2024, 2025 e 2027) e nas bordas do periodo:
         2025-12-31 23:59:59 (fora), 2026-01-01 00:00:00 (dentro),
         2026-12-31 23:59:59 (dentro) e 2027-01-01 00:00:00 (fora).
     - Itens com PrecoUnitario diferente do Preco atual do produto
         (preco historico registrado no momento do pedido).
     - Produto com estoque baixo (Id 10) para testar estoque insuficiente.
     - ValorTotal de cada pedido = SUM(Quantidade * PrecoUnitario).

   Ranking esperado para 2026-01-01 a 2026-12-31 (top 5):
     Cliente 1, 2, 3, 7 e 4 (nesta ordem).

   Observacao: o estoque dos produtos representa o saldo atual e nao e
   debitado pelos pedidos historicos deste seed.
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE [MiniGestaoPedidos];
GO

IF EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    PRINT N'03-seed-data: tabela Clientes ja possui dados. Seed ignorado.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    /* ---------- Clientes ---------- */
    SET IDENTITY_INSERT dbo.Clientes ON;

    INSERT INTO dbo.Clientes (Id, Nome, Email, DataCadastro) VALUES
        ( 1, N'João Silva',      N'joao.silva@exemplo.com',      '2025-01-10T09:00:00'),
        ( 2, N'Maria Souza',     N'maria.souza@exemplo.com',     '2025-01-15T10:30:00'),
        ( 3, N'Carlos Pereira',  N'carlos.pereira@exemplo.com',  '2025-02-01T14:00:00'),
        ( 4, N'Ana Oliveira',    N'ana.oliveira@exemplo.com',    '2025-02-20T11:15:00'),
        ( 5, N'João Silva',      N'joao.silva.junior@exemplo.com', '2025-03-05T16:45:00'),
        ( 6, N'Maria Souza',     N'maria.souza.lima@exemplo.com',  '2025-03-18T08:20:00'),
        ( 7, N'Pedro Santos',    N'pedro.santos@exemplo.com',    '2025-04-02T13:10:00'),
        ( 8, N'Fernanda Lima',   N'fernanda.lima@exemplo.com',   '2025-04-22T15:30:00'),
        ( 9, N'Rafael Costa',    N'rafael.costa@exemplo.com',    '2025-05-11T10:00:00'),
        (10, N'Juliana Alves',   N'juliana.alves@exemplo.com',   '2025-06-01T09:45:00'),
        (11, N'Lucas Ribeiro',   N'lucas.ribeiro@exemplo.com',   '2025-06-25T17:00:00'),
        (12, N'Beatriz Martins', N'beatriz.martins@exemplo.com', '2025-07-14T12:30:00');

    SET IDENTITY_INSERT dbo.Clientes OFF;

    /* ---------- Produtos ---------- */
    SET IDENTITY_INSERT dbo.Produtos ON;

    INSERT INTO dbo.Produtos (Id, Nome, Preco, Estoque) VALUES
        ( 1, N'Notebook 15"',        4500.00,  25),
        ( 2, N'Mouse sem fio',         89.90, 200),
        ( 3, N'Teclado mecânico',     349.90, 120),
        ( 4, N'Monitor 24"',         1199.00,  60),
        ( 5, N'Headset gamer',        259.90,  80),
        ( 6, N'Webcam HD',            199.90,  90),
        ( 7, N'Cadeira ergonômica',  1299.00,  15),
        ( 8, N'Hub USB-C',            149.90, 150),
        ( 9, N'SSD 1TB',              499.90,  70),
        (10, N'Cabo HDMI 2m',          29.90,   3);

    SET IDENTITY_INSERT dbo.Produtos OFF;

    /* ---------- Pedidos (ValorTotal e calculado apos os itens) ---------- */
    SET IDENTITY_INSERT dbo.Pedidos ON;

    INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido, ValorTotal) VALUES
        -- 2026 (dentro do periodo)
        ( 1, 1, '2026-01-05T10:15:00', 0),
        ( 2, 2, '2026-01-12T14:30:00', 0),
        ( 3, 3, '2026-01-20T09:00:00', 0),
        ( 4, 4, '2026-02-03T16:45:00', 0),
        ( 5, 1, '2026-02-14T11:20:00', 0),
        ( 6, 5, '2026-02-25T13:10:00', 0),
        ( 7, 6, '2026-03-02T08:50:00', 0),
        ( 8, 7, '2026-03-10T15:05:00', 0),
        ( 9, 2, '2026-03-18T17:30:00', 0),
        (10, 8, '2026-03-27T10:40:00', 0),
        (11, 3, '2026-04-04T12:00:00', 0),
        (12, 9, '2026-04-15T09:25:00', 0),
        (13, 1, '2026-04-28T14:15:00', 0),
        (14, 4, '2026-05-06T11:45:00', 0),
        (15, 5, '2026-05-19T16:20:00', 0),
        (16, 2, '2026-06-01T10:10:00', 0),
        (17, 7, '2026-06-13T13:35:00', 0),
        (18, 3, '2026-06-24T15:50:00', 0),
        (19, 6, '2026-07-07T09:40:00', 0),
        (20, 8, '2026-07-21T12:25:00', 0),
        (21, 1, '2026-08-05T14:55:00', 0),
        (22, 9, '2026-08-18T10:05:00', 0),
        (23, 4, '2026-09-02T11:30:00', 0),
        (24, 2, '2026-09-16T16:00:00', 0),
        (25, 3, '2026-10-01T09:15:00', 0),
        (26, 7, '2026-10-14T13:45:00', 0),
        (27, 5, '2026-11-03T15:10:00', 0),
        (28, 1, '2026-11-12T10:50:00', 0),
        -- Bordas do periodo 2026 (dentro)
        (29, 3, '2026-01-01T00:00:00', 0),
        (30, 9, '2026-12-31T23:59:59', 0),
        -- Fora do periodo 2026
        (31, 1, '2025-12-31T23:59:59', 0),
        (32, 2, '2025-11-20T10:00:00', 0),
        (33, 3, '2025-08-08T14:00:00', 0),
        (34, 4, '2025-03-15T09:30:00', 0),
        (35, 7, '2025-01-10T17:00:00', 0),
        (36, 9, '2027-01-01T00:00:00', 0),
        (37, 1, '2027-02-10T11:00:00', 0),
        (38, 2, '2027-03-22T15:30:00', 0),
        (39, 5, '2027-05-05T10:20:00', 0),
        (40, 8, '2024-12-15T12:00:00', 0);

    SET IDENTITY_INSERT dbo.Pedidos OFF;

    /* ---------- Itens dos pedidos ---------- */
    INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES
        ( 1,  1, 1, 4500.00), ( 1,  2, 2,   89.90),
        ( 2,  4, 2, 1199.00), ( 2,  3, 1,  349.90),
        ( 3,  7, 1, 1299.00), ( 3,  5, 1,  259.90),
        ( 4,  3, 1,  349.90), ( 4,  2, 1,   89.90),
        ( 5,  4, 1, 1199.00), ( 5,  9, 2,  499.90), ( 5, 10, 3,   29.90),
        ( 6,  5, 1,  259.90), ( 6,  8, 2,  149.90),
        ( 7,  2, 1,   89.90), ( 7, 10, 2,   29.90),
        ( 8,  1, 1, 4300.00), ( 8,  6, 1,  199.90),  -- preco historico do notebook
        ( 9,  1, 1, 4500.00), ( 9,  8, 1,  149.90),
        (10,  9, 1,  499.90), (10,  6, 2,  199.90),
        (11,  4, 1, 1199.00), (11,  3, 2,  349.90),
        (12,  2, 3,   89.90),
        (13,  7, 1, 1299.00), (13,  5, 2,  259.90),
        (14,  9, 1,  499.90), (14,  8, 1,  149.90),
        (15,  3, 1,  349.90),
        (16,  7, 1, 1299.00), (16,  9, 1,  499.90), (16,  2, 1,   89.90),
        (17,  4, 1, 1199.00), (17,  5, 1,  259.90),
        (18,  1, 1, 4500.00),
        (19,  8, 1,  149.90), (19, 10, 1,   29.90),
        (20,  3, 1,  349.90), (20,  2, 1,   89.90),
        (21,  1, 1, 4500.00), (21,  3, 1,  349.90),
        (22,  6, 1,  199.90), (22,  5, 1,  259.90),
        (23,  4, 1, 1199.00), (23,  2, 2,   89.90),
        (24,  9, 2,  499.90), (24,  6, 1,  199.90),
        (25,  8, 2,  149.90), (25,  2, 1,   89.90),
        (26,  7, 1, 1299.00),
        (27,  2, 2,   89.90), (27, 10, 1,   29.90),
        (28,  9, 1,  499.90), (28,  8, 1,  149.90),
        (29,  5, 1,  259.90), (29, 10, 2,   29.90),
        (30,  3, 1,  349.90), (30,  8, 1,  149.90),
        (31,  1, 2, 4400.00),
        (32,  4, 1, 1199.00), (32,  5, 1,  259.90),
        (33,  7, 1, 1299.00),
        (34,  3, 2,  349.90),
        (35,  2, 1,   89.90), (35, 10, 1,   29.90),
        (36,  1, 1, 4500.00),
        (37,  4, 2, 1199.00),
        (38,  1, 1, 4500.00), (38,  9, 1,  499.90),
        (39,  7, 1, 1299.00),
        (40,  6, 1,  199.90), (40,  2, 1,   89.90);

    /* ---------- ValorTotal = soma dos itens ---------- */
    UPDATE p
       SET p.ValorTotal = t.Total
      FROM dbo.Pedidos AS p
      INNER JOIN (SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total
                    FROM dbo.ItensPedido
                   GROUP BY PedidoId) AS t
              ON t.PedidoId = p.Id;

    COMMIT TRANSACTION;

    PRINT N'03-seed-data: dados de exemplo inseridos com sucesso.';
END
GO
