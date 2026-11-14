/* =============================================================================
   optional/create-readonly-user.sql
   Cria um login/usuario SOMENTE LEITURA (db_datareader) para uso do MCP do
   Cursor no banco MiniGestaoPedidos. Script OPCIONAL, fora da sequencia 01..03.

   Pre-requisito: 01-create-tables.sql executado (banco MiniGestaoPedidos existe).

   A senha NAO fica no arquivo: informe-a pela variavel do sqlcmd (-v).
   Se a variavel nao for informada, o sqlcmd aborta com erro de variavel nao definida.
   Evite aspas simples (') na senha.

   Exemplo (a partir da raiz do repositorio):
     docker cp database/optional/create-readonly-user.sql mini-gestao-sqlserver:/tmp/ro.sql
     docker exec mini-gestao-sqlserver bash -c '/opt/mssql-tools18/bin/sqlcmd -C -b -f 65001 -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -v MCP_READONLY_PASSWORD="<SenhaForte#123>" -i /tmp/ro.sql'

   Idempotente: pode ser executado novamente (atualiza a senha do login).
   ============================================================================= */

SET NOCOUNT ON;
GO

USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'mcp_readonly')
BEGIN
    EXEC (N'CREATE LOGIN [mcp_readonly] WITH PASSWORD = N''$(MCP_READONLY_PASSWORD)'', CHECK_POLICY = ON, DEFAULT_DATABASE = [MiniGestaoPedidos];');
END
ELSE
BEGIN
    EXEC (N'ALTER LOGIN [mcp_readonly] WITH PASSWORD = N''$(MCP_READONLY_PASSWORD)'';');
END
GO

USE [MiniGestaoPedidos];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'mcp_readonly')
BEGIN
    CREATE USER [mcp_readonly] FOR LOGIN [mcp_readonly];
END
GO

-- Somente leitura: SELECT em todas as tabelas do banco.
IF IS_ROLEMEMBER(N'db_datareader', N'mcp_readonly') <> 1
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER [mcp_readonly];
END
GO

PRINT N'Usuario mcp_readonly (db_datareader) configurado em MiniGestaoPedidos.';
GO
