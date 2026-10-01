using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Queries;
using TaskManagement.Domain.Repository;
using TaskManagement.Infra.Context;

namespace TaskManagement.Infra.Repositories;

public sealed class TaskItemRepository(DataContext db) : ITaskItemRepository
{
    public async Task AddAsync(TaskItem taskItem, CancellationToken ct)
        => await db.TaskItems.AddAsync(taskItem, ct);

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.TaskItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IEnumerable<TaskItem>> ListAsync(TaskFilter filter, CancellationToken ct)
        => await db.TaskItems
            .AsNoTracking()
            .Where(TaskQueries.Matches(filter))
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

    public Task UpdateAsync(TaskItem taskItem, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        db.TaskItems.Update(taskItem);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem item, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        db.TaskItems.Remove(item);
        return Task.CompletedTask;
    }

    public async Task SaveAsync(CancellationToken ct)
        => await db.SaveChangesAsync(ct);
}
