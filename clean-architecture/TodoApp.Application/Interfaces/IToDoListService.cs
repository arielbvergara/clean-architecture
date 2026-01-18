using TodoApp.Domain.Entities;

namespace TodoApp.Application.Interfaces;

public interface IToDoListService
{
    Task<ToDoList> GetListByIdAsync(int id);
    Task AddListAsync(ToDoList list);
}