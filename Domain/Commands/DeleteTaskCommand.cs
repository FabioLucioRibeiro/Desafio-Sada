using TaskManagement.Domain.Commands.Contracts;

namespace TaskManagement.Domain.Commands;

public sealed record DeleteTaskCommand(Guid Id) : ICommand
{
    public void Validate() { }
}
