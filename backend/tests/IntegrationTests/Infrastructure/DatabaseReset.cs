using Microsoft.Data.SqlClient;

namespace IntegrationTests.Infrastructure;

internal static class DatabaseReset
{
    /// <summary>
    /// Limpa as tabelas (ordem de FK), reinicia identities e reaplica o seed 03.
    /// </summary>
    public static async Task ResetAsync(SqlServerFixture fixture)
    {
        await fixture.ComConexaoAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SET NOCOUNT ON;
                DELETE FROM dbo.ItensPedido;
                DELETE FROM dbo.Pedidos;
                DELETE FROM dbo.Produtos;
                DELETE FROM dbo.Clientes;
                DBCC CHECKIDENT ('dbo.ItensPedido', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Pedidos', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Produtos', RESEED, 0);
                DBCC CHECKIDENT ('dbo.Clientes', RESEED, 0);
                """;
            await command.ExecuteNonQueryAsync();
        });

        await fixture.AplicarScriptsAsync("03-seed-data.sql");
    }

    public static async Task<int> ScalarIntAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public static async Task<decimal> ScalarDecimalAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToDecimal(result);
    }
}
