using System.Runtime.ExceptionServices;
using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

/// <summary>
/// Tratamento global de exceções, convertendo-as em respostas ProblemDetails (RFC 7807).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Cliente cancelou a requisição: nada a responder.
            _logger.LogInformation("Requisição cancelada pelo cliente: {Path}", context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Erro após o início da resposta em {Path}", context.Request.Path);
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException validation:
                problem = new ValidationProblemDetails(
                    validation.Errors.ToDictionary(e => e.Key, e => e.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dados inválidos.",
                    Detail = validation.Message,
                };
                break;

            case DomainException domain:
                problem = Create(StatusCodes.Status400BadRequest, "Regra de negócio violada.", domain.Message);
                break;

            case NotFoundException notFound:
                problem = Create(StatusCodes.Status404NotFound, "Recurso não encontrado.", notFound.Message);
                break;

            case EstoqueInsuficienteException estoque:
                problem = Create(StatusCodes.Status409Conflict, "Estoque insuficiente.", estoque.Message);
                problem.Extensions["produtoId"] = estoque.ProdutoId;
                problem.Extensions["solicitado"] = estoque.Solicitado;
                problem.Extensions["disponivel"] = estoque.Disponivel;
                break;

            case BadHttpRequestException badRequest:
                problem = Create(StatusCodes.Status400BadRequest, "Requisição inválida.", badRequest.Message);
                break;

            default:
                _logger.LogError(exception, "Erro inesperado ao processar {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                problem = Create(
                    StatusCodes.Status500InternalServerError,
                    "Erro interno do servidor.",
                    "Ocorreu um erro inesperado. Tente novamente mais tarde.");
                break;
        }

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        // Usa o tipo concreto para serializar também o "errors" do ValidationProblemDetails.
        await context.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json");
    }

    private static ProblemDetails Create(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
        };
}
