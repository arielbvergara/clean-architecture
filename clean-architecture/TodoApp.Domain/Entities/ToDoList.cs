namespace TodoApp.Domain.Entities;

public class ToDoList
{
    public int ToDoListId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}