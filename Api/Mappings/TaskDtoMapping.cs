using TaskManagement.Api.DTOs;
using TaskManagement.Domain.Commands;

namespace TaskManagement.Api.Mappings;

public static class TaskDtoMapping
{
    public static CreateTaskCommand ToCreateCommand(this SaveTaskDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        DueDate = dto.DueDate,
        Status = dto.Status
    };

    public static UpdateTaskCommand ToUpdateCommand(this SaveTaskDto dto, Guid id) => new()
    {
        Id = id,
        Title = dto.Title,
        Description = dto.Description,
        DueDate = dto.DueDate,
        Status = dto.Status
    };
}
