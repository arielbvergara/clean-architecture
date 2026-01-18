using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Configurations;

public class ToDoListConfiguration : IEntityTypeConfiguration<ToDoList>
{
    public void Configure(EntityTypeBuilder<ToDoList> builder)
    {
        builder.HasData(
            new ToDoList { ToDoListId = 1, Title = "John's Personal Tasks", Description = "John's personal task list", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new ToDoList { ToDoListId = 2, Title = "Jane's Work Tasks", Description = "Jane's work-related tasks", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
        );
    }
}