namespace TaskManagement.Domain.Exceptions;

public sealed class TaskNotFoundException(Guid id) : Exception($"Tarefa {id} não encontrada.");
