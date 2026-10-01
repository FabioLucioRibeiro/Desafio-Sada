using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Mappings;
using TaskManagement.Domain.Commands;
using TaskManagement.Domain.DTOs;
using TaskManagement.Domain.Handlers.Contracts;
using TaskManagement.Domain.Queries;

namespace TaskManagement.Api.Controllers;

/// <summary>Cadastro, consulta, atualização e exclusão de tarefas.</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public sealed class TasksController(ITaskHandler handler, ILogger<TasksController> logger) : ControllerBase
{
    /// <summary>Lista as tarefas em ordem crescente de código.</summary>
    /// <remarks>
    /// Todos os filtros são opcionais e combinados com E.
    /// Search busca no título e na descrição sem diferenciar maiúsculas de minúsculas.
    /// Status aceita Pending, InProgress ou Completed.
    /// DueDate inclui vencimentos até a data informada (inclusive) e exclui tarefas sem vencimento.
    /// Exemplo: /api/tasks?search=reunião&amp;status=Pending&amp;dueDate=2026-10-10
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> List([FromQuery] TaskFilter filter, CancellationToken ct)
        => Ok(await handler.ListAsync(filter, ct));

    /// <summary>Obtém uma tarefa pelo seu ID (GUID).</summary>
    /// <response code="404">Não existe tarefa com o ID informado.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken ct)
        => Ok(await handler.GetAsync(id, ct));

    /// <summary>Cria uma tarefa e retorna seu ID e código numérico gerado.</summary>
    /// <remarks>Id, Code e CreatedAt são gerados pelo servidor e não fazem parte da entrada.</remarks>
    /// <response code="201">Tarefa criada. O cabeçalho Location contém sua URL.</response>
    /// <response code="400">Título ausente, limites excedidos, status ou data inválidos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(SaveTaskDto request, CancellationToken ct)
    {
        var task = await handler.HandleAsync(request.ToCreateCommand(), ct);
        logger.LogInformation("Tarefa {TaskId} criada com código {TaskCode}", task.Id, task.Code);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    /// <summary>Substitui título, descrição, vencimento e status de uma tarefa.</summary>
    /// <remarks>Preserva ID, código e data de criação. Campos opcionais omitidos ficam nulos; status omitido assume Pending.</remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Update(Guid id, SaveTaskDto request, CancellationToken ct)
    {
        var task = await handler.HandleAsync(request.ToUpdateCommand(id), ct);
        logger.LogInformation("Tarefa {TaskId} atualizada", id);
        return Ok(task);
    }

    /// <summary>Exclui uma tarefa pelo seu ID.</summary>
    /// <response code="204">Tarefa excluída, sem corpo na resposta.</response>
    /// <response code="404">Não existe tarefa com o ID informado.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteTaskCommand(id), ct);
        logger.LogInformation("Tarefa {TaskId} excluída", id);
        return NoContent();
    }
}
