/* =============================================================================
   01-create-tables.sql
   Mini Gestao de Pedidos - criacao do banco e das tabelas.

   Idempotente: pode ser executado varias vezes sem erro.
   Execucao: sqlcmd -C -b -S localhost -U sa -P <senha> -i 01-create-tables.sql
   ============================================================================= */

SET NOCOUNT ON;
GO

IF DB_ID(N'MiniGestaoPedidos') IS NULL
BEGIN
    CREATE DATABASE [MiniGestaoPedidos];
END
GO

USE [MiniGestaoPedidos];
GO

/* ---------- Clientes ---------- */
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id           INT IDENTITY(1,1) NOT NULL,
        Nome         NVARCHAR(150)     NOT NULL,
        Email        NVARCHAR(150)     NOT NULL,
        DataCadastro DATETIME2         NOT NULL
            CONSTRAINT DF_Clientes_DataCadastro DEFAULT SYSDATETIME(),
        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id)
    );
END
GO

/* ---------- Produtos ---------- */
IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id      INT IDENTITY(1,1) NOT NULL,
        Nome    NVARCHAR(150)     NOT NULL,
        Preco   DECIMAL(18,2)     NOT NULL,
        Estoque INT               NOT NULL,
        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Produtos_Preco   CHECK (Preco > 0),
        CONSTRAINT CK_Produtos_Estoque CHECK (Estoque >= 0)
    );
END
GO

/* ---------- Pedidos ---------- */
IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos
    (
        Id         INT IDENTITY(1,1) NOT NULL,
        ClienteId  INT               NOT NULL,
        DataPedido DATETIME2         NOT NULL
            CONSTRAINT DF_Pedidos_DataPedido DEFAULT SYSDATETIME(),
        ValorTotal DECIMAL(18,2)     NOT NULL,
        CONSTRAINT PK_Pedidos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId)
            REFERENCES dbo.Clientes (Id)
    );
END
GO

/* ---------- ItensPedido ---------- */
IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido
    (
        Id            INT IDENTITY(1,1) NOT NULL,
        PedidoId      INT               NOT NULL,
        ProdutoId     INT               NOT NULL,
        Quantidade    INT               NOT NULL,
        PrecoUnitario DECIMAL(18,2)     NOT NULL,
        CONSTRAINT PK_ItensPedido PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId)
            REFERENCES dbo.Pedidos (Id),
        CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId)
            REFERENCES dbo.Produtos (Id),
        CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0)
    );
END
GO
