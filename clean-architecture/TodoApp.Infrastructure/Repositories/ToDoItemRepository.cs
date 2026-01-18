using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

/// <summary>
/// This talks to database hence as per Clean Architecture we should write here
/// </summary>
public class ToDoItemRepository(AppDbContext context) : IToDoItemRepository
{
    public async Task AddItemAsync(ToDoItem item)
    {
        context.ToDoItems.Add(item);
        await context.SaveChangesAsync();
    }
    public async Task DeleteItemAsync(int id)
    {
        var item = await context.ToDoItems.FindAsync(id);
        if (item != null)
        {
            context.ToDoItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ToDoItem>> GetAllItemsAsync()
    {
        return await context.ToDoItems.ToListAsync();
    }

    public async Task<ToDoItem?> GetItemByIdAsync(int id)
    {
        return await context.ToDoItems.FindAsync(id);
    }

    public async Task<IEnumerable<ToDoItem>> GetItemsByListIdAsync(int toDoListId)
    {
        return await context.ToDoItems.Where(i => i.ToDoListId == toDoListId).ToListAsync();
    }

    public async Task UpdateItemAsync(ToDoItem item)
    {
        context.ToDoItems.Update(item);
        await context.SaveChangesAsync();
    }
}