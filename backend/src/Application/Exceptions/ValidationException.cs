namespace Application.Exceptions;

/// <summary>Dados de entrada inválidos (HTTP 400).</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message)
        : this(new Dictionary<string, string[]> { [string.Empty] = new[] { message } }, message)
    {
    }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors, string? message = null)
        : base(message ?? "Um ou mais erros de validação ocorreram.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
