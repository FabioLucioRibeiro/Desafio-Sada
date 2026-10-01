namespace TaskManagement.Domain.Entities;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public bool Equals(Entity? other)
        => other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
