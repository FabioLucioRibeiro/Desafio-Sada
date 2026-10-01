using TaskItemStatus = TaskManagement.Domain.Enum.TaskItemStatus;
namespace TaskManagement.Domain.DTOs;
public sealed record TaskDto(Guid Id, string Title, string? Description, DateOnly? DueDate, TaskItemStatus Status, DateOnly CreatedAt, int Code);
