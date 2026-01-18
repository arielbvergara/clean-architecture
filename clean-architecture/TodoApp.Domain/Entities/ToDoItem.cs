using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

public class ToDoItem
{
    public int ToDoItemId { get; set; }
    public int ToDoListId { get; set; }      // Foreign Key to ToDoList
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public PriorityLevel Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}