using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Commands.Contracts;
using TaskManagement.Domain.Enum;

namespace TaskManagement.Domain.Commands;

public abstract class TaskCommand : ICommand
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    public DateOnly? DueDate { get; init; }

    [EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus Status { get; init; } = TaskItemStatus.Pending;

    public void Validate()
        => Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
