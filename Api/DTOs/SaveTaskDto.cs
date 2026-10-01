using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enum;

namespace TaskManagement.Api.DTOs;

public sealed class SaveTaskDto
{

    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;


    [StringLength(4000)]
    public string? Description { get; init; }


    public DateOnly? DueDate { get; init; }

    [EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus Status { get; init; } = TaskItemStatus.Pending;
}
