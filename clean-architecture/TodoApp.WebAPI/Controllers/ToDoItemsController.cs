using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Interfaces;
using TodoApp.Domain.Entities;

namespace TodoApp.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ToDoItemController(IToDoItemService toDoItemService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ToDoItem>>> GetAll()
    {
        var response = await toDoItemService.GetAllItemsAsync();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ToDoItem>> GetById(int id)
    {
        var item = await toDoItemService.GetItemByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult> Create(ToDoItem item)
    {
        await toDoItemService.AddItemAsync(item);
        return CreatedAtAction(nameof(GetById), new { id = item.ToDoItemId }, item);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, ToDoItem item)
    {
        if (id != item.ToDoItemId) return BadRequest();
        await toDoItemService.UpdateItemAsync(item);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        await toDoItemService.DeleteItemAsync(id);
        return NoContent();
    }
}