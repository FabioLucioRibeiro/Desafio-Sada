using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Commands;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enum;
using TaskManagement.Domain.Exceptions;
using TaskManagement.Domain.Handlers;
using TaskManagement.Domain.Queries;
using TaskManagement.Domain.Repository;

namespace TaskManagement.Tests.Unit;

public sealed class TaskHandlerTests
{
    private readonly FakeRepository repository = new();
    private TaskHandler Handler => new(repository);

    [Fact]
    public async Task Create_NormalizesFieldsAndSavesOnce()
    {
        var result = await Handler.HandleAsync(new CreateTaskCommand
        {
            Title = "  Reunião  ", Description = "  Planejamento  "
        });
        Assert.Equal("Reunião", result.Title);
        Assert.Equal("Planejamento", result.Description);
        Assert.Equal(TaskItemStatus.Pending, result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Null(result.DueDate);
        Assert.Single(repository.Items);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_RejectsMissingOrBlankTitle(string? title)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Handler.HandleAsync(
            new CreateTaskCommand { Title = title! }));
        Assert.Empty(repository.Items);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Create_RejectsTitleLongerThan200()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Handler.HandleAsync(
            new CreateTaskCommand { Title = new string('a', 201) }));
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Create_RejectsDescriptionLongerThan4000()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Handler.HandleAsync(
            new CreateTaskCommand { Title = "Tarefa", Description = new string('a', 4001) }));
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Create_RejectsUndefinedStatus()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Handler.HandleAsync(
            new CreateTaskCommand { Title = "Tarefa", Status = (TaskItemStatus)99 }));
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Create_AcceptsMaximumLengthsAndPastDueDate()
    {
        var result = await Handler.HandleAsync(new CreateTaskCommand
        {
            Title = new string('a', 200), Description = new string('b', 4000),
            DueDate = new DateOnly(2000, 1, 1)
        });
        Assert.Equal(200, result.Title.Length);
        Assert.Equal(4000, result.Description!.Length);
        Assert.Equal(new DateOnly(2000, 1, 1), result.DueDate);
    }

    [Fact]
    public async Task Update_ChangesEditableFieldsAndPreservesIdentity()
    {
        var item = new TaskItem("Antes", "Descrição", null, TaskItemStatus.Pending);
        repository.Items.Add(item);
        var createdAt = item.CreatedAt;
        var result = await Handler.HandleAsync(new UpdateTaskCommand
        {
            Id = item.Id, Title = "  Depois  ", Description = "   ",
            DueDate = new DateOnly(2026, 10, 10), Status = TaskItemStatus.Completed
        });
        Assert.Equal(item.Id, result.Id);
        Assert.Equal(item.Code, result.Code);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal("Depois", result.Title);
        Assert.Null(result.Description);
        Assert.Equal(new DateOnly(2026, 10, 10), result.DueDate);
        Assert.Equal(TaskItemStatus.Completed, result.Status);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("Título", 99)]
    public async Task Update_RejectsInvalidInputWithoutChangingEntity(string title, int status)
    {
        var item = new TaskItem("Antes", null, null, TaskItemStatus.Pending);
        repository.Items.Add(item);
        await Assert.ThrowsAsync<ValidationException>(() => Handler.HandleAsync(new UpdateTaskCommand
        {
            Id = item.Id, Title = title, Status = (TaskItemStatus)status
        }));
        Assert.Equal("Antes", item.Title);
        Assert.Equal(TaskItemStatus.Pending, item.Status);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Get_ReturnsExistingTask()
    {
        var item = new TaskItem("Existente", null, null, TaskItemStatus.Pending);
        repository.Items.Add(item);
        Assert.Equal(item.Id, (await Handler.GetAsync(item.Id)).Id);
    }

    [Fact]
    public async Task MissingTask_ThrowsForGetUpdateAndDelete()
    {
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<TaskNotFoundException>(() => Handler.GetAsync(id));
        await Assert.ThrowsAsync<TaskNotFoundException>(() => Handler.HandleAsync(
            new UpdateTaskCommand { Id = id, Title = "Válido" }));
        await Assert.ThrowsAsync<TaskNotFoundException>(() => Handler.HandleAsync(new DeleteTaskCommand(id)));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Delete_RemovesExistingTaskAndSavesOnce()
    {
        var item = new TaskItem("Excluir", null, null, TaskItemStatus.Pending);
        repository.Items.Add(item);
        await Handler.HandleAsync(new DeleteTaskCommand(item.Id));
        Assert.Empty(repository.Items);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task List_RejectsUndefinedStatus()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Handler.ListAsync(
            new TaskFilter { Status = (TaskItemStatus)99 }));
    }

    private sealed class FakeRepository : ITaskItemRepository
    {
        public List<TaskItem> Items { get; } = [];
        public int SaveCount { get; private set; }
        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<IEnumerable<TaskItem>> ListAsync(TaskFilter filter, CancellationToken ct)
            => Task.FromResult<IEnumerable<TaskItem>>(Items.Where(TaskQueries.Matches(filter).Compile()).ToArray());
        public Task AddAsync(TaskItem item, CancellationToken ct)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(TaskItem item, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteAsync(TaskItem item, CancellationToken ct)
        {
            Items.Remove(item);
            return Task.CompletedTask;
        }
        public Task SaveAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
