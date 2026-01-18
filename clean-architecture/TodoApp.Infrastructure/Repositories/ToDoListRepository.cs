using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

public class ToDoListRepository(AppDbContext context) : IToDoListRepository
{
    public async Task<IEnumerable<ToDoList>> GetAllListsAsync()
    {
        return await context.ToDoLists.ToListAsync();
    }

    public async Task<ToDoList?> GetListByIdAsync(int id)
    {
        return await context.ToDoLists.FindAsync(id);
    }

    public async Task AddListAsync(ToDoList list)
    {
        context.ToDoLists.Add(list);
        await context.SaveChangesAsync();
    }

    public async Task UpdateListAsync(ToDoList list)
    {
        context.ToDoLists.Update(list);
        await context.SaveChangesAsync();
    }

    public async Task DeleteListAsync(int id)
    {
        var list = await context.ToDoLists.FindAsync(id);
        if (list != null)
        {
            context.ToDoLists.Remove(list);
            await context.SaveChangesAsync();
        }
    }
}