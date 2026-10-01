using TaskManagement.Domain.Enum;
namespace TaskManagement.Domain.Entities;


public sealed class TaskItem : Entity
{
    public TaskItem(
        string title,
        string? description,
        DateOnly? dueDate,
        TaskItemStatus status
        )
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
        Status = status;
        CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateOnly CreatedAt { get; private set; }
    public int Code { get; private set; }

    public void Update(
        string title,
        string? description,
        DateOnly? dueDate,
        TaskItemStatus status
        )
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
        Status = status;
    }
}
