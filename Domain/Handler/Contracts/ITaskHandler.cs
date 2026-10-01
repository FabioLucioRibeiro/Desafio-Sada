using TaskManagement.Domain.Commands;
using TaskManagement.Domain.DTOs;
using TaskManagement.Domain.Queries;

namespace TaskManagement.Domain.Handlers.Contracts;

public interface ITaskHandler
{
    Task<IReadOnlyList<TaskDto>> ListAsync(TaskFilter filter, CancellationToken ct = default);
    Task<TaskDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<TaskDto> HandleAsync(CreateTaskCommand request, CancellationToken ct = default);
    Task<TaskDto> HandleAsync(UpdateTaskCommand request, CancellationToken ct = default);
    Task HandleAsync(DeleteTaskCommand request, CancellationToken ct = default);
}
