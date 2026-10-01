using System.Linq.Expressions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Queries;

public static class TaskQueries
{
    public static Expression<Func<TaskItem, bool>> Matches(TaskFilter filter)
    {
        var text = filter.Search?.Trim();
        return task => (!filter.Status.HasValue || task.Status == filter.Status.Value)
            && (!filter.DueDate.HasValue ||
                (task.DueDate.HasValue && task.DueDate.Value <= filter.DueDate.Value))
            && (string.IsNullOrEmpty(text)
                || task.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (task.Description != null
                    && task.Description.Contains(text, StringComparison.OrdinalIgnoreCase)));
    }
}
