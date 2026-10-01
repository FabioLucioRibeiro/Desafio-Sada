using System.ComponentModel.DataAnnotations;
using TaskStatus = TaskManagement.Domain.Enum.TaskItemStatus;
namespace TaskManagement.Domain.Queries;
public sealed class TaskFilter
{
    [StringLength(200)] public string? Search { get; init; }
    [EnumDataType(typeof(TaskStatus))] public TaskStatus? Status { get; init; }
    public DateOnly? DueDate { get; init; }
}
