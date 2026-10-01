using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Queries;

namespace TaskManagement.Domain.Repository;

public interface ITaskItemRepository
{
    Task<IEnumerable<TaskItem>> ListAsync(TaskFilter filter, CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(TaskItem taskItem, CancellationToken ct);
    Task DeleteAsync(TaskItem item, CancellationToken ct);
    Task UpdateAsync(TaskItem taskItem, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
