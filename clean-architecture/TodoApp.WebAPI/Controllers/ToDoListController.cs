using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToDoListController(IToDoListService toDoListService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateToDoList([FromBody] ToDoList toDoList)
    {
        await toDoListService.AddListAsync(toDoList);
        return CreatedAtAction(nameof(GetToDoList), new { id = toDoList.ToDoListId }, toDoList);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetToDoList(int id)
    {
        var toDoList = await toDoListService.GetListByIdAsync(id);
        return toDoList == null ? NotFound() : Ok(toDoList);
    }
}