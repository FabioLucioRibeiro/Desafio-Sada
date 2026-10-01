using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Mappings;
using TaskManagement.Domain.Commands;
using TaskManagement.Domain.DTOs;
using TaskManagement.Domain.Handlers.Contracts;
using TaskManagement.Domain.Queries;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public sealed class TasksController(ITaskHandler handler, ILogger<TasksController> logger) : ControllerBase
{

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> List([FromQuery] TaskFilter filter, CancellationToken ct)
        => Ok(await handler.ListAsync(filter, ct));


    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Get(Guid id, CancellationToken ct)
        => Ok(await handler.GetAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(SaveTaskDto request, CancellationToken ct)
    {
        var task = await handler.HandleAsync(request.ToCreateCommand(), ct);
        logger.LogInformation("Tarefa {TaskId} criada com código {TaskCode}", task.Id, task.Code);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }


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
