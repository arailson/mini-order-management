using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Sobe um SQL Server 2022 real em Docker (uma vez por execução), aplica os scripts 01-03
/// e expõe factory + connection string.
///
/// Observação: a lib Testcontainers.MsSql permanece referenciada no .csproj (conforme o plano),
/// mas neste ambiente (Windows + SDK/.NET 11) o Docker.DotNet falha no named pipe
/// (NotImplementedException em DockerPipeStream.Flush). A fixture usa a CLI do Docker
/// com o mesmo ciclo de vida (start → scripts → dispose).
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string SaPassword = "Test_Str0ng!Pass123";
    public const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    private readonly string _containerName = $"mini-gestao-it-{Guid.NewGuid():N}"[..28];
    private int _hostPort;

    public string ConnectionString { get; private set; } = string.Empty;

    public string ScriptsDirectory { get; private set; } = string.Empty;

    public ApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!await DockerDisponivelAsync())
        {
            throw new InvalidOperationException(
                "Docker não está disponível. Os testes de integração exigem o Docker Desktop em execução.");
        }

        _hostPort = ObterPortaLivre();
        ScriptsDirectory = LocateScriptsDirectory();

        await DockerAsync(
            "run", "-d",
            "--name", _containerName,
            "-e", "ACCEPT_EULA=Y",
            "-e", $"MSSQL_SA_PASSWORD={SaPassword}",
            "-e", "MSSQL_PID=Developer",
            "-p", $"{_hostPort}:1433",
            Image);

        await AguardarProntoAsync();
        await AplicarScriptsAsync("01-create-tables.sql", "02-create-indexes.sql", "03-seed-data.sql");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"localhost,{_hostPort}",
            InitialCatalog = "MiniGestaoPedidos",
            UserID = "sa",
            Password = SaPassword,
            TrustServerCertificate = true,
            Encrypt = false,
        };
        ConnectionString = builder.ConnectionString;
        Factory = new ApiFactory(ConnectionString);
        _ = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        try
        {
            await DockerAsync("rm", "-f", _containerName);
        }
        catch
        {
            // best-effort no teardown
        }
    }

    public async Task AplicarScriptsAsync(params string[] nomes)
    {
        foreach (var nome in nomes)
        {
            var local = Path.Combine(ScriptsDirectory, nome);
            if (!File.Exists(local))
            {
                throw new FileNotFoundException($"Script SQL não encontrado: {local}");
            }

            var remoto = $"/tmp/{nome}";
            await DockerAsync("cp", local, $"{_containerName}:{remoto}");

            var (exit, stdout, stderr) = await DockerAsync(
                "exec",
                "-e", $"SQLCMDPASSWORD={SaPassword}",
                _containerName,
                "/opt/mssql-tools18/bin/sqlcmd",
                "-C", "-b", "-f", "65001",
                "-S", "localhost",
                "-U", "sa",
                "-i", remoto);

            if (exit != 0)
            {
                throw new InvalidOperationException(
                    $"Falha ao executar {nome} (exit {exit}): {stdout}{stderr}");
            }
        }
    }

    public async Task<T> ComConexaoAsync<T>(Func<SqlConnection, Task<T>> action)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return await action(connection);
    }

    public Task ComConexaoAsync(Func<SqlConnection, Task> action) =>
        ComConexaoAsync(async connection =>
        {
            await action(connection);
            return 0;
        });

    private async Task AguardarProntoAsync()
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var (exit, _, _) = await DockerAsync(
                    "exec",
                    "-e", $"SQLCMDPASSWORD={SaPassword}",
                    _containerName,
                    "/opt/mssql-tools18/bin/sqlcmd",
                    "-C", "-b",
                    "-S", "localhost",
                    "-U", "sa",
                    "-Q", "SELECT 1");

                if (exit == 0)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                last = ex;
            }

            await Task.Delay(2000);
        }

        throw new TimeoutException(
            $"SQL Server no container {_containerName} não ficou pronto a tempo.", last);
    }

    private static async Task<bool> DockerDisponivelAsync()
    {
        try
        {
            var (exit, _, _) = await DockerAsync("info");
            return exit == 0;
        }
        catch
        {
            return false;
        }
    }

    private static int ObterPortaLivre()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> DockerAsync(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Não foi possível iniciar o processo docker.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string LocateScriptsDirectory()
    {
        var fromOutput = Path.Combine(AppContext.BaseDirectory, "database");
        if (Directory.Exists(fromOutput) && Directory.EnumerateFiles(fromOutput, "01-*.sql").Any())
        {
            return fromOutput;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "database");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "01-create-tables.sql")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Pasta database/ com os scripts 01-03 não encontrada (nem no output nem a partir da raiz).");
    }
}
