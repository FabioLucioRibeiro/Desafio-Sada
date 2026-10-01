using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            TaskNotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro inesperado. TraceId: {TraceId}", context.TraceIdentifier);
        else
            logger.LogWarning("Requisição rejeitada: {Message}", exception.Message);

        ProblemDetails problem = exception is ValidationException validation
            ? new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [string.Empty] = [validation.Message]
            })
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = status switch
        {
            404 => "Tarefa não encontrada",
            400 => "Dados inválidos",
            _ => "Erro interno"
        };
        problem.Detail = status == 500 ? "Não foi possível processar a solicitação." : exception.Message;
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem,
            options: null, contentType: "application/problem+json", cancellationToken: ct);
        return true;
    }
}
