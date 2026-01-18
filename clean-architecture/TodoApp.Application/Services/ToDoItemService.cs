using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Services;

/// <summary>
/// All the business logic should go via service hence we need to call repository from servcie and service
/// should be in Application layer.
/// </summary>
public class ToDoItemService(IToDoItemRepository toDoItemRepository) : IToDoItemService
{
    public async Task<IEnumerable<ToDoItem>> GetAllItemsAsync()
    {
        return await toDoItemRepository.GetAllItemsAsync();
    }

    public async Task<ToDoItem?> GetItemByIdAsync(int id)
    {
        return await toDoItemRepository.GetItemByIdAsync(id); ;
    }

    public async Task AddItemAsync(ToDoItem item)
    {
        await toDoItemRepository.AddItemAsync(item);
    }

    public async Task UpdateItemAsync(ToDoItem item)
    {
        await toDoItemRepository.UpdateItemAsync(item);
    }

    public async Task DeleteItemAsync(int id)
    {
        await toDoItemRepository.DeleteItemAsync(id);
    }
}