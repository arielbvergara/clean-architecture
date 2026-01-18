using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;

namespace TodoApp.Infrastructure.Configurations;

public class ToDoItemConfiguration : IEntityTypeConfiguration<ToDoItem>
{
    public void Configure(EntityTypeBuilder<ToDoItem> builder)
    {
        builder.HasData(
            new ToDoItem { ToDoItemId = 1, ToDoListId = 1, Title = "Buy groceries", Description = "Buy milk, eggs, and bread", DueDate = DateTime.Now.AddDays(1), IsCompleted = false, Priority = PriorityLevel.Medium, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new ToDoItem { ToDoItemId = 2, ToDoListId = 1, Title = "Schedule doctor appointment", Description = "Annual health checkup", DueDate = DateTime.Now.AddDays(3), IsCompleted = false, Priority = PriorityLevel.High, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new ToDoItem { ToDoItemId = 3, ToDoListId = 2, Title = "Complete project report", Description = "Finish and submit the quarterly report", DueDate = DateTime.Now.AddDays(2), IsCompleted = false, Priority = PriorityLevel.High, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
            new ToDoItem { ToDoItemId = 4, ToDoListId = 2, Title = "Team meeting", Description = "Discuss project progress with the team", DueDate = DateTime.Now.AddDays(1), IsCompleted = false, Priority = PriorityLevel.Low, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
        );
    }
}