using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory apontando a connection string para o SQL Server em container criado pela fixture.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            });
        });
    }
}
