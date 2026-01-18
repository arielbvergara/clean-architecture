using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.Application.Services;

public class ToDoListService(IToDoListRepository toDoListRepository) : IToDoListService
{
    public async Task<IEnumerable<ToDoList>> GetAllListsAsync()
    {
        return await toDoListRepository.GetAllListsAsync();
    }

    public async Task<ToDoList> GetListByIdAsync(int id)
    {
        return await toDoListRepository.GetListByIdAsync(id);
    }

    public async Task AddListAsync(ToDoList list)
    {
        await toDoListRepository.AddListAsync(list);
    }

    public async Task UpdateListAsync(ToDoList list)
    {
        await toDoListRepository.UpdateListAsync(list);
    }

    public async Task DeleteListAsync(int id)
    {
        await toDoListRepository.DeleteListAsync(id);
    }
}