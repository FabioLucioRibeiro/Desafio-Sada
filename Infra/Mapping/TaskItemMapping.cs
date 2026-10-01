using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infra.Mapping;

public sealed class TaskItemMapping : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();

        builder.Property(task => task.Title).IsRequired().HasMaxLength(200);
        builder.Property(task => task.Description).HasMaxLength(4000).IsRequired(false);
        builder.Property(task => task.DueDate).IsRequired(false);
        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.Status).HasConversion<int>().IsRequired();

        builder.Property(task => task.Code).ValueGeneratedOnAdd();
        builder.HasAlternateKey(task => task.Code);
    }
}
