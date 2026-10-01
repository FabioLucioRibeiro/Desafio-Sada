using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enum;
using TaskManagement.Domain.Queries;
using TaskManagement.Infra.Context;
using TaskManagement.Infra.Repositories;

namespace TaskManagement.Tests.Integration;

public sealed class TaskRepositoryTests
{
    private static DbContextOptions<DataContext> NewOptions()
        => new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    [Fact]
    public async Task Codes_ArePositiveAndDistinctAcrossContextsAndAfterDeletion()
    {
        var options = NewOptions();
        var codes = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            await using var db = new DataContext(options);
            var repository = new TaskItemRepository(db);
            var task = new TaskItem("Tarefa", null, null, TaskItemStatus.Pending);
            await repository.AddAsync(task, default);
            await repository.SaveAsync(default);
            Assert.True(task.Code > 0);
            codes.Add(task.Code);
            if (i == 1)
            {
                await repository.DeleteAsync(task, default);
                await repository.SaveAsync(default);
            }
        }
        Assert.Equal(3, codes.Distinct().Count());
        Assert.True(codes[1] > codes[0]);
        Assert.True(codes[2] > codes[1]);
    }

    [Fact]
    public async Task Update_OnlyPersistsAtSaveAndPreservesGeneratedCode()
    {
        var options = NewOptions();
        await using var db = new DataContext(options);
        var repository = new TaskItemRepository(db);
        var task = new TaskItem("Antes", null, null, TaskItemStatus.Pending);
        await repository.AddAsync(task, default);
        await repository.SaveAsync(default);
        var code = task.Code;
        var createdAt = task.CreatedAt;
        task.Update("Depois", "Descrição", new DateOnly(2026, 10, 1), TaskItemStatus.Completed);
        await repository.UpdateAsync(task, default);
        await using (var beforeSave = new DataContext(options))
            Assert.Equal("Antes", (await beforeSave.TaskItems.SingleAsync()).Title);
        await repository.SaveAsync(default);
        await using var afterSave = new DataContext(options);
        var persisted = await afterSave.TaskItems.SingleAsync();
        Assert.Equal("Depois", persisted.Title);
        Assert.Equal(TaskItemStatus.Completed, persisted.Status);
        Assert.Equal(code, persisted.Code);
        Assert.Equal(createdAt, persisted.CreatedAt);
    }

    [Theory]
    [InlineData(null, null, null, "A,B,C,D")]
    [InlineData(null, 0, null, "A,C")]
    [InlineData(null, null, 10, "A,B")]
    [InlineData(" REUNIÃO ", null, null, "A,C")]
    [InlineData("cliente", null, null, "B")]
    [InlineData("   ", null, null, "A,B,C,D")]
    [InlineData(null, 0, 10, "A")]
    [InlineData("reunião", 0, 10, "A")]
    [InlineData("ausente", null, null, "")]
    public async Task List_AppliesOptionalAndCombinedFilters(string? search, int? status, int? day, string expected)
    {
        await using var db = new DataContext(NewOptions());
        var tasks = new[]
        {
            new TaskItem("Reunião A", null, new DateOnly(2026, 10, 1), TaskItemStatus.Pending),
            new TaskItem("Relatório B", "Cliente", new DateOnly(2026, 10, 10), TaskItemStatus.Completed),
            new TaskItem("Reunião C", null, new DateOnly(2026, 10, 11), TaskItemStatus.Pending),
            new TaskItem("Entrega D", null, null, TaskItemStatus.InProgress)
        };
        db.TaskItems.AddRange(tasks);
        await db.SaveChangesAsync();
        var result = (await new TaskItemRepository(db).ListAsync(new TaskFilter
        {
            Search = search,
            Status = status.HasValue ? (TaskItemStatus)status.Value : null,
            DueDate = day.HasValue ? new DateOnly(2026, 10, day.Value) : null
        }, default)).ToArray();
        Assert.Equal(expected, string.Join(",", result.Select(x => x.Title[^1])));
        Assert.Equal(result.Select(x => x.Code).Order(), result.Select(x => x.Code));
    }
}
