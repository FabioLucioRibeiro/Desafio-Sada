using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Commands;
using TaskManagement.Domain.DTOs;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Handlers.Contracts;
using TaskManagement.Domain.Queries;
using TaskManagement.Domain.Repository;

namespace TaskManagement.Domain.Handlers;

public sealed class TaskHandler(ITaskItemRepository repository) : ITaskHandler
{
    public async Task<IReadOnlyList<TaskDto>> ListAsync(TaskFilter filter, CancellationToken ct = default)
    {
        Validator.ValidateObject(filter, new ValidationContext(filter), validateAllProperties: true);
        return (await repository.ListAsync(filter, ct)).Select(Map).ToArray();
    }

    public async Task<TaskDto> GetAsync(Guid id, CancellationToken ct = default)
        => Map(await FindAsync(id, ct));

    public async Task<TaskDto> HandleAsync(CreateTaskCommand request, CancellationToken ct = default)
    {
        request.Validate();
        var item = new TaskItem(request.Title.Trim(), NormalizeDescription(request.Description),
            request.DueDate, request.Status);

        await repository.AddAsync(item, ct);
        await repository.SaveAsync(ct);
        return Map(item);
    }

    public async Task<TaskDto> HandleAsync(UpdateTaskCommand request, CancellationToken ct = default)
    {
        request.Validate();
        var item = await FindAsync(request.Id, ct);
        item.Update(request.Title.Trim(), NormalizeDescription(request.Description),
            request.DueDate, request.Status);

        await repository.UpdateAsync(item, ct);
        await repository.SaveAsync(ct);
        return Map(item);
    }

    public async Task HandleAsync(DeleteTaskCommand request, CancellationToken ct = default)
    {
        request.Validate();
        var item = await FindAsync(request.Id, ct);
        await repository.DeleteAsync(item, ct);
        await repository.SaveAsync(ct);
    }

    private async Task<TaskItem> FindAsync(Guid id, CancellationToken ct)
        => await repository.GetByIdAsync(id, ct) ?? throw new TaskNotFoundException(id);

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static TaskDto Map(TaskItem item)
        => new(item.Id, item.Title, item.Description, item.DueDate, item.Status, item.CreatedAt, item.Code);
}
