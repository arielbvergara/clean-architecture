using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface IToDoListRepository
{
    Task<IEnumerable<ToDoList>> GetAllListsAsync();
    Task<ToDoList?> GetListByIdAsync(int id);
    Task AddListAsync(ToDoList list);
    Task UpdateListAsync(ToDoList list);
    Task DeleteListAsync(int id);
}