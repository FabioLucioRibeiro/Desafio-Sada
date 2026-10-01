namespace TaskManagement.Domain.Commands;

public sealed class UpdateTaskCommand : TaskCommand
{
    public Guid Id { get; init; }
}
