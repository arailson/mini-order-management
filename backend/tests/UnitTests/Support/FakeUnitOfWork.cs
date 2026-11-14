using Application.Abstractions;

namespace UnitTests.Support;

/// <summary>
/// Unit of Work em memória: executa a ação, registra commit/rollback
/// e propaga a exceção (sem banco).
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public int CommitCount { get; private set; }

    public int RollbackCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await action(cancellationToken);
            CommitCount++;
            return result;
        }
        catch
        {
            RollbackCount++;
            throw;
        }
    }
}
